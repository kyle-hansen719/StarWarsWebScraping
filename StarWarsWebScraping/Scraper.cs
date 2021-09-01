using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using StarWarsWebScraping.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StarWarsWebScraping
{
    class Scraper
    {
        private readonly ChromeDriver _driver;

        private readonly string WookiepeediaBaseUrl = "https://starwars.fandom.com/wiki";

        public Scraper()
        {
            // Make sure ChromeDriver.exe is in the StarWarsWebScraping folder
            _driver = new ChromeDriver(Environment.CurrentDirectory.Replace("\\bin\\Debug\\netcoreapp3.1", ""));
        }

        // Returns all characters and their information
        public List<Character> GetAllCharacters()
        {
            var characters = new List<Character>();

            GetAllArticleUrls().ForEach(x =>
            {
                try
                {
                    var character = GetCharacter(x);
                    characters.Add(character);
                }
                catch (NotFoundException exception)
                {
                    // GetCharacter throws NotFound if the article is not a character article.
                }
            });


            _driver.Close();
            return characters;
        }

        // gets a characters page and returns their information
        private Character GetCharacter(string url)
        {
            _driver.Navigate().GoToUrl(url);

            // TODO: Fix this (checking if the article is a character article)
            var infoTab = _driver.FindElementByXPath("//*[@id=\"mw-content-text\"]/div/aside");
            if (!infoTab.Text.Contains("Gender")) throw new NotFoundException("This article is not a character article.");

            return new Character
            {
                CharacterName = _driver.FindElementById("firstHeading").Text,
                Url = url
            };
        }

        // Takes the div that represents the main text box for characters
        private List<CharacterRelationship> GetCharacterRelationships(IWebElement characterInfoDiv)
        {
            // TODO: Do this
            var relationships = new List<CharacterRelationship>();

            return relationships;
        }

        // Gets the urls of all articles on Wookieepedia
        private List<string> GetAllArticleUrls()
        {
            var urls = new List<string>();

            string nextPageUrl = $"{WookiepeediaBaseUrl}/Special:AllPages";
            // TODO: reinstate this while loop
            //var page = GetArticlePage(nextPageUrl);
            //urls.AddRange(page.ArticleUrls);

            while (nextPageUrl != null)
            {
                var page = GetArticlePage(nextPageUrl);
                urls.AddRange(page.ArticleUrls);
                nextPageUrl = page.NextPageUrl;
            }

            Console.WriteLine("Finished Getting All Urls");
            return urls;
        }

        // Gets a single page of urls and returns the url to the next page
        private (string NextPageUrl, IEnumerable<string> ArticleUrls) GetArticlePage(string url)
        {
            _driver.Navigate().GoToUrl(url);

            // fix problem with looping back and forth between pages
            var nextPageUrl = _driver.FindElementByXPath("//*[@id=\"mw-content-text\"]/div[2]/a[1]").GetAttribute("href");

            //var articleUrls = _driver.FindElementByClassName("mw-allpages-chunk")
            //    .FindElements(By.TagName("li"))
            //    .ToList()
            //    .Select(x => x.FindElement(By.TagName("a")).GetAttribute("href"));

            var articleUrls = GetAllArticleLinksFromBody(_driver.FindElementByClassName("mw-allpages-chunk").GetAttribute("innerHTML"), 0);

            return (nextPageUrl, articleUrls);
        }

        private List<string> GetAllArticleLinksFromBody(string htmlBody, int numRuns, List<string> urls = null)
        {
            urls ??= new List<string>();
            var hrefTagString = "href=\"";

            // TODO: this code is bad and confusing

            // remove numRuns
            if (!htmlBody.Contains(hrefTagString) || numRuns > 20) return urls;

            var openingIndex = htmlBody.IndexOf(hrefTagString);

            ////
            //var hrefTagLength = hrefTagString.Length;
            //var opening = openingIndex + hrefTagString.Length;
            //var length = htmlBody.Length;
            //var closing = htmlBody.Length - openingIndex + hrefTagString.Length;
            //var substring = htmlBody.Substring(openingIndex + hrefTagString.Length, htmlBody.Length - openingIndex + hrefTagString.Length);
            ////

            var closingIndex = htmlBody.Substring(openingIndex + hrefTagString.Length).IndexOf("\" ");

            // openingIndex + closingIndex
            urls.Add(htmlBody.Substring(openingIndex + hrefTagString.Length, closingIndex));

            return GetAllArticleLinksFromBody(htmlBody.Substring(closingIndex), numRuns, urls);
        }

        // TODO: Get character details
    }
}
