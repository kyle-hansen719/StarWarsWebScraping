using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using StarWarsWebScraping.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace StarWarsWebScraping
{
    class Scraper
    {
        private readonly List<DriverWithId> _drivers;
        private readonly StarWarsContext _context;

        private readonly string WookiepeediaBaseUrl = "https://starwars.fandom.com";

        public Scraper(List<DriverWithId> drivers, StarWarsContext context)
        {
            _drivers = drivers;
            _context = context;
        }

        public void CloseDriver()
        {
            _drivers.ForEach(x => x.Driver.Close());
        }

        // Returns all characters articles
        public void GetAllCharacters(int numArticlePages = int.MaxValue)
        {
            //// checks if data exists in the database already
            //var lastCharacterUrl = _context.Characters.OrderBy(x => x.Id).Select(x => x.Url).LastOrDefault();
            //var lastArticleCheckedId = _context.ArticleUrls.Where(x => x.Url == lastCharacterUrl).Select(x => x.Id).FirstOrDefault();
            //var articleUrls = _context.ArticleUrls.Any() 
            //    ? _context.ArticleUrls.Where(x => x.Id > lastArticleCheckedId).Select(x => x.Url).ToList() 
            //    : GetAllArticleUrls(numArticlePages);

            if (!_context.ArticleUrls.Any())
            {
                GetAllArticleUrls(numArticlePages);
            }

            var articleUrls = _context.ArticleUrls.ToList();

            if (articleUrls.Count > 0)
            {
                var articleUrlsGroups = _drivers
                .Select(x => articleUrls.Where(y => y.Id % _drivers.Count == x.Id));

                Task.WaitAll(articleUrlsGroups
                    .Select(x => GetCharactersAsync(x, GetDriverFromIterator(x.First().Id)))
                    .ToArray());
            }
        }

        private async Task GetCharactersAsync(IEnumerable<ArticleUrl> articleUrls, ChromeDriver driver)
        {
            foreach (var articleUrl in articleUrls)
            {
                await Task.Run(() => GetCharacter(articleUrl, driver));
            }
        }

        // gets a characters page and returns their information
        private Character GetCharacter(ArticleUrl article, ChromeDriver driver)
        {
            driver.Navigate().GoToUrl(article.Url);

            try
            {
                var infoTab = driver.FindElementByXPath("//*[@id=\"mw-content-text\"]/div/aside");
                if (!infoTab.Text.Contains("Gender")) return null;

                var character = new Character
                {
                    CharacterName = driver.FindElementById("firstHeading").Text,
                    Url = article.Url
                };

                _context.Characters.Add(character);
                _context.SaveChanges();

                return character;
            }
            catch
            {
                return null;
            }
        }

        // Gets the urls of all articles on Wookieepedia
        // TODO: I think this has to be synchronous
        private void GetAllArticleUrls(int numPages)
        {
            var urls = new List<string>();
            var driver = _drivers.Select(x => x.Driver).First();

            var nextPageUrl = $"{WookiepeediaBaseUrl}/wiki/Special:AllPages";

            var i = 0;
            while (nextPageUrl != null && i < numPages)
            {
                var page = GetArticlePage(nextPageUrl, i == 0, driver);
                urls.AddRange(page.ArticleUrls);
                nextPageUrl = page.NextPageUrl;
                i += 1;
            }

            _context.ArticleUrls.AddRange(urls.Select(x => new ArticleUrl { Url = x }));
            _context.SaveChanges();

            Console.WriteLine("Finished Getting All Urls");
        }

        // Gets a single page of urls and returns the url to the next page
        private (string NextPageUrl, IEnumerable<string> ArticleUrls) GetArticlePage(string url, bool isFirstPage, ChromeDriver driver)
        {
            driver.Navigate().GoToUrl(url);

            string nextPageUrl = null;
            try
            {
                // TODO: replace this xpath call with another selector if i can
                nextPageUrl = driver.FindElementByXPath("//*[@id=\"mw-content-text\"]/div[2]/a[2]").GetAttribute("href");
            }
            catch
            {
                // On the first page, the next page button is this but on the last page this is the previous button so I need to only set next page to this
                // when this is the first page
                if (isFirstPage) nextPageUrl = driver.FindElementByXPath("//*[@id=\"mw-content-text\"]/div[2]/a[1]").GetAttribute("href");
            }

            var articleUrls = GetAllTagsFromBody(driver
                .FindElementByClassName("mw-allpages-chunk")
                .GetAttribute("innerHTML")
                .Replace("\n", "")
                .Replace("\r", ""), "href");

            return (nextPageUrl, articleUrls);
        }

        // TODO: Make this run in parallel with all drivers
        public void GetCharacterRelationships()
        {
            var characters = _context.Characters.ToList();
            for (var i = 0; i < characters.Count; i++)
            {
                var driver = GetDriverFromIterator(i);

                driver.Navigate().GoToUrl(characters[i].Url);

                var characterHyperlinks = GetAllTagsFromBody(driver
                    .FindElementByClassName("mw-parser-output")
                    .GetAttribute("innerHTML")
                    .Replace("\n", "")
                    .Replace("\r", ""), "href")
                    .Select(x => $"('{x}')");

                var query = $@"
                    DECLARE @characterId INT
                        SET @characterId = {characters[i].Id}
                    IF OBJECT_ID('tempdb..#hyperlinks') IS NOT NULL DROP TABLE #hyperlinks
                    CREATE TABLE #hyperlinks (
                        hyperlink NVARCHAR(MAX)
                    );

                    INSERT INTO #hyperlinks (hyperlink)
                    	VALUES {string.Join(',', characterHyperlinks)}
                    
                    INSERT INTO
                        dbo.Relationships(CharacterId, HyperlinkCharacterId)
                    SELECT
                        @characterId,
                        Id
                    FROM
                        dbo.Characters
                    WHERE
                        Url IN(SELECT* FROM #hyperlinks)";

                _context.Database.ExecuteSqlRaw(query);
            }
        }

        // htmlTag is just "href", "a", etc.
        private List<string> GetAllTagsFromBody(string htmlBody, string htmlTag, List<string> urls = null)
        {
            urls ??= new List<string>();
            var hrefTagString = $"{htmlTag}=\"";
            if (!htmlBody.Contains(hrefTagString)) return urls;

            var openingIndex = htmlBody.IndexOf(hrefTagString) + hrefTagString.Length;
            // Url length is first instance of '" ' after the first instance of 'href="'
            var urlLength = htmlBody.Substring(openingIndex).IndexOf("\"");

            urls.Add(WookiepeediaBaseUrl + htmlBody.Substring(openingIndex, urlLength));

            return GetAllTagsFromBody(htmlBody.Substring(openingIndex + urlLength), htmlTag, urls);
        }

        private ChromeDriver GetDriverFromIterator(int iterator)
        {
            return _drivers[iterator % _drivers.Count].Driver;
        }
    }
}
