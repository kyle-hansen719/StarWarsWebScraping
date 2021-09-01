using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StarWarsWebScraping
{
    class Program
    {

        static void Main(string[] args)
        {
            var scraper = new Scraper();

            var urls = new List<string>();

            string nextPageUrl = "https://starwars.fandom.com/wiki/Special:AllPages";
            while (nextPageUrl != null)
            {
                var page = scraper.GetArticlePage(nextPageUrl);
                urls.AddRange(page.ArticleUrls);

                urls.ForEach(x => Console.WriteLine(x));

                nextPageUrl = page.NextPageUrl;
            }

            File.WriteAllLines("urls.txt", urls);

            Console.ReadLine();
        }
    }

    public class Scraper
    {
        private ChromeDriver _driver;

        private string WookiepeediaBaseUrl = "https://starwars.fandom.com/wiki/";

        public Scraper()
        {
            _driver = new ChromeDriver();
        }

        // Returns all characters and their information
        public List<Character> GetAllCharacters()
        {
            _driver.Close();

            throw new NotImplementedException();
        }

        // gets a characters page and returns their information
        private Character GetCharacterPage(string url)
        {
            // TODO: Do this
            var driver = new ChromeDriver(url);

            throw new NotImplementedException();
        }

        // Gets the urls of all articles on Wookieepedia
        private List<string> GetUrls()
        {
            // TODO: get all character urls and names
            var urls = new List<string>();

            throw new NotImplementedException();

        }

        // Gets a single page of urls and returns the url to the next page
        public (string NextPageUrl, IEnumerable<string> ArticleUrls) GetArticlePage(string url)
        {
            _driver.Navigate().GoToUrl(url);
            var nextPageUrl = _driver.FindElementByXPath("//*[@id=\"mw-content-text\"]/div[2]/a[1]").GetAttribute("href");

            var articleUrls = _driver.FindElementByClassName("mw-allpages-chunk")
                .FindElements(By.TagName("li"))
                .ToList()
                .Select(x => x.FindElement(By.TagName("a")).GetAttribute("href"));

            return (nextPageUrl, articleUrls);
        }
        
        // Removes non-character urls from the list of urls
        private List<string> FilterCharacterUrls(List<string> urls)
        {
            throw new NotImplementedException();
        }
    }

    public class Character
    {
        public int Id { get; set; }
        public string CharacterName { get; set; }
        public string Url { get; set; }

        public List<int> Characters { get; set; }
    }
}
