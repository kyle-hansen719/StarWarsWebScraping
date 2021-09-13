using OpenQA.Selenium.Chrome;
using System;
using System.Linq;

namespace StarWarsWebScraping
{
    class Program
    {
        private static readonly int NumBrowserWindows = 4;

        static void Main(string[] args)
        {
            // Sets up Selenium with chromedriver.exe
            // Make sure ChromeDriver.exe is in the StarWarsWebScraping folder
            var options = new ChromeOptions();
            var rootPath = Environment.CurrentDirectory.Replace("\\bin\\Debug\\netcoreapp3.1", "");
            // Loads adblock to speed up page loading
            options.AddExtension(rootPath + "\\uBlock-Origin_v1.37.2.crx");

            var drivers = Enumerable
                .Range(0, NumBrowserWindows)
                .Select(x => new DriverWithId { Id = x, Driver = new ChromeDriver(rootPath, options) })
                .ToList();
            using var context = new StarWarsContext();

            var scraper = new Scraper(drivers, context);
            scraper.GetAllCharacters();
            scraper.GetCharacterRelationships();
            scraper.CloseDriver();
        }
    }

    public class DriverWithId
    {
        public ChromeDriver Driver { get; set; }
        public int Id { get; set; }
    }
}
