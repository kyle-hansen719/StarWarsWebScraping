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
        private readonly RemoteWebDriver _driver;

        private readonly string WookiepeediaBaseUrl = "https://starwars.fandom.com";

        public Scraper(RemoteWebDriver driver)
        {
            _driver = driver;
        }

        public void CloseDriver()
        {
            _driver.Close();
        }

        // Returns all characters articles
        public List<Character> GetAllCharacters(int numArticlePages = int.MaxValue)
        {
            var characters = GetAllArticleUrls(numArticlePages)
                .Select(x => GetCharacter(x))
                .Where(x => x != null)
                .ToList();

            return characters;
        }

        // gets a characters page and returns their information
        private Character GetCharacter(string url)
        {
            _driver.Navigate().GoToUrl(url);

            // TODO: Fix this (checking if the article is a character article)
            try
            {
                var infoTab = _driver.FindElementByXPath("//*[@id=\"mw-content-text\"]/div/aside");
                if (!infoTab.Text.Contains("Gender")) return null;
            }
            catch
            {
                return null;
            }

            return new Character
            {
                CharacterName = _driver.FindElementById("firstHeading").Text,
                Url = url
            };
        }

        // Gets the urls of all articles on Wookieepedia
        private List<string> GetAllArticleUrls(int numPages)
        {
            var urls = new List<string>();

            string nextPageUrl = $"{WookiepeediaBaseUrl}/wiki/Special:AllPages";

            // TODO: Get rid of i
            var i = 0;
            while (nextPageUrl != null && i < numPages)
            {
                var page = GetArticlePage(nextPageUrl, i == 0);
                urls.AddRange(page.ArticleUrls);
                nextPageUrl = page.NextPageUrl;
                i += 1;
            }

            Console.WriteLine("Finished Getting All Urls");
            return urls;
        }

        // Gets a single page of urls and returns the url to the next page
        private (string NextPageUrl, IEnumerable<string> ArticleUrls) GetArticlePage(string url, bool isFirstPage)
        {
            _driver.Navigate().GoToUrl(url);

            string nextPageUrl = null;
            try
            {
                // TODO: replace this xpath call with another selector if i can
                nextPageUrl = _driver.FindElementByXPath("//*[@id=\"mw-content-text\"]/div[2]/a[2]").GetAttribute("href");
            }
            catch
            {
                // On the first page, the next page button is this but on the last page this is the previous button so I need to only set next page to this
                // when this is the first page
                if (isFirstPage) nextPageUrl = _driver.FindElementByXPath("//*[@id=\"mw-content-text\"]/div[2]/a[1]").GetAttribute("href");
            }

            var articleUrls = GetAllTagsFromBody(_driver
                .FindElementByClassName("mw-allpages-chunk")
                .GetAttribute("innerHTML")
                .Replace("\n", "")
                .Replace("\r", ""), "href");

            return (nextPageUrl, articleUrls);
        }

        public void GetCharacterRelationships()
        {
            using var context = new StarWarsContext();

            var characters = context.Characters.ToList();
            foreach (var character in characters)
            {
                _driver.Navigate().GoToUrl(character.Url);

                // TODO: benchmark linq to entities vs normal linq for hyperlink to character link
                // TODO: find a way to narrow down the character text search
                var characterHyperlinks = GetAllTagsFromBody(_driver
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

                context.Database.ExecuteSqlRaw(query);
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
    }
}
