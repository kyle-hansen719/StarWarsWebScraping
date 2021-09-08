using OpenQA.Selenium.Chrome;
using StarWarsWebScraping.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Concurrent;

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
            if (!_context.ArticleUrls.Any())
            {
                GetAllArticleUrls(numArticlePages);
            }

            var articleUrls = _context.ArticleUrls.FromSqlRaw(@"
                SELECT		[Id], [Url]
                FROM		dbo.ArticleUrls
                WHERE		Id > (SELECT TOP 1 AU.Id FROM dbo.Characters C INNER JOIN dbo.ArticleUrls AU ON C.Url = AU.Url ORDER BY C.Id DESC)
            ").OrderByDescending(x => x.Id).AsEnumerable();

            _articleUrls = new ConcurrentStack<ArticleUrl>(articleUrls);

            Task.WaitAll(_drivers.Select(x => GetCharactersAsync(x.Driver)).ToArray());
        }

        private ConcurrentStack<ArticleUrl> _articleUrls;

        private async Task GetCharactersAsync(ChromeDriver driver)
        {
            while (_articleUrls.TryPop(out var articleUrl))
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


        // RELATIONSHIPS
        public void GetCharacterRelationships()
        {
            var maxCharacterId = _context.Relationships.Max(x => x.CharacterId);
            var characters = _context.Characters.Where(x => x.Id > maxCharacterId).OrderByDescending(x => x.Id).AsEnumerable();
            
            _characters = new ConcurrentStack<Character>(characters);

            Task.WaitAll(_drivers.Select(x => GetRelationshipsAsync(x.Driver)).ToArray());
        }

        private ConcurrentStack<Character> _characters;

        private async Task GetRelationshipsAsync(ChromeDriver driver)
        {
            while (_characters.TryPop(out var character))
            {
                await Task.Run(() => GetRelationship(character, driver));
            }
        }

        private void GetRelationship(Character character, ChromeDriver driver)
        {
            driver.Navigate().GoToUrl(character.Url);

            var characterHyperlinks = GetAllTagsFromBody(driver
                .FindElementByClassName("mw-parser-output")
                .GetAttribute("innerHTML")
                .Replace("\n", "")
                .Replace("\r", ""), "href")
                .Select(x => $"('{x}')");

            var query = $@"
                    DECLARE @characterId INT
                        SET @characterId = {character.Id}
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
