using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using StarWarsWebScraping.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

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
        public List<Character> GetAllCharacters(int numArticlePages = int.MaxValue)
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

            var characters = articleUrls
                .Select(x => GetCharacter(x))
                .Where(x => x != null)
                .ToList();

            return characters;
        }

        // gets a characters page and returns their information
        private Character GetCharacter(ArticleUrl article)
        {
            var driver = GetDriverFromIterator(article.Id);

            driver.Navigate().GoToUrl(article.Url);

            // TODO: Fix this (checking if the article is a character article)
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
        private void GetAllArticleUrls(int numPages)
        {
            var urls = new List<string>();

            string nextPageUrl = $"{WookiepeediaBaseUrl}/wiki/Special:AllPages";

            var i = 0;
            while (nextPageUrl != null && i < numPages)
            {
                var driver = GetDriverFromIterator(i);

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

        public void GetCharacterRelationships()
        {
            var characters = _context.Characters.ToList();
            for (var i = 0; i < characters.Count; i++)
            {
                var driver = GetDriverFromIterator(i);

                driver.Navigate().GoToUrl(characters[i].Url);

                // TODO: benchmark linq to entities vs normal linq for hyperlink to character link
                // TODO: find a way to narrow down the character text search
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
            // TODO: this code is bad and confusing
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
