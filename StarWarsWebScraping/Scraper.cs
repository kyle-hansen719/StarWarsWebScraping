using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using StarWarsWebScraping.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StarWarsWebScraping
{
    class Scraper
    {
        private readonly RemoteWebDriver _driver;

        private readonly string WookiepeediaBaseUrl = "https://starwars.fandom.com/wiki";

        public Scraper(RemoteWebDriver driver)
        {
            _driver = driver;
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

                    // Status Log
                    Console.WriteLine($"Got: {character.CharacterName}");
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
        private List<CharacterRelationship> GetCharacterRelationships(List<string> urls)
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
            var nextPageUrl = _driver.FindElementByXPath("//*[@id=\"mw-content-text\"]/div[2]/a[1]").GetAttribute("href");

            // TODO: Use custom HTML parsing to speed this up.
            var articleUrls = _driver.FindElementByClassName("mw-allpages-chunk")
                .FindElements(By.TagName("li"))
                .ToList()
                .Select(x => x.FindElement(By.TagName("a")).GetAttribute("href"));

            return (nextPageUrl, articleUrls);
        }
    }
}
