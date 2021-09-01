﻿using OpenQA.Selenium;
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
        //public (List<Character> Characters, List<CharacterRelationship> Relationships) GetAllCharacters()
        //{
        //    var articleUrls = GetAllArticleUrls();

        //    var characters = new List<Character>();
        //    var relationships = new List<CharacterRelationship>();

        //    foreach (var url in articleUrls)
        //    {
        //        try
        //        {
        //            var character = GetCharacter(url);
        //            characters.Add(character.Character);
        //            relationships.AddRange(character.Relationships);
        //        }
        //        catch (NotFoundException exception)
        //        {
        //            // GetCharacter throws NotFound if the article is not a character article.
        //            continue;
        //        }
        //    }

        //    _driver.Close();
        //    return (characters, relationships);
        //}

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

        //private (Character Character, List<CharacterRelationship> Relationships) GetCharacter(string url)
        //{
        //    _driver.Navigate().GoToUrl(url);

        //    // TODO: Fix this (checking if the article is a character article)
        //    var infoTab = _driver.FindElementByXPath("//*[@id=\"mw-content-text\"]/div/aside");
        //    if (!infoTab.Text.Contains("Gender")) throw new NotFoundException("This article is not a character article.");

        //    var relationships = GetCharacterRelationships();

        //    var character = new Character
        //    {
        //        CharacterName = _driver.FindElementById("firstHeading").Text,
        //        Url = url,
        //    };

        //    return (character, relationships);
        //}

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
            var page = GetArticlePage(nextPageUrl);
            urls.AddRange(page.ArticleUrls);

            //while (nextPageUrl != null)
            //{
            //    var page = GetArticlePage(nextPageUrl);
            //    urls.AddRange(page.ArticleUrls);
            //    nextPageUrl = page.NextPageUrl;
            //}

            Console.WriteLine("Finished Getting All Urls");
            return urls;
        }

        // Gets a single page of urls and returns the url to the next page
        private (string NextPageUrl, IEnumerable<string> ArticleUrls) GetArticlePage(string url)
        {
            _driver.Navigate().GoToUrl(url);
            var nextPageUrl = _driver.FindElementByXPath("//*[@id=\"mw-content-text\"]/div[2]/a[1]").GetAttribute("href");

            var articleUrls = _driver.FindElementByClassName("mw-allpages-chunk")
                .FindElements(By.TagName("li"))
                .ToList()
                .Select(x => x.FindElement(By.TagName("a")).GetAttribute("href"));

            return (nextPageUrl, articleUrls);
        }
    }
}
