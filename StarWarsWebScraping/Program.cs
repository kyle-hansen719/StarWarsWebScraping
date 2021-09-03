using OpenQA.Selenium.Chrome;
using StarWarsWebScraping.Entities;
using System;

namespace StarWarsWebScraping
{
    class Program
    {

        static void Main(string[] args)
        {
            // Sets up Selenium with chromedriver.exe
            // Make sure ChromeDriver.exe is in the StarWarsWebScraping folder
            var scraper = new Scraper(new ChromeDriver(Environment.CurrentDirectory.Replace("\\bin\\Debug\\netcoreapp3.1", "")));

            using var context = new StarWarsContext();
            context.Characters.AddRange(scraper.GetAllCharacters());
            context.SaveChanges();

            // Make sure GetRelationships is called after GetCharacters because relationships requires db data
            // might want to fix this later
            scraper.GetCharacterRelationships();

            scraper.CloseDriver();
        }
    }
}
