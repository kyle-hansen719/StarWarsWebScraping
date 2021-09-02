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

            // Change this to get all characters then get their relationships.
            var characters = scraper.GetAllCharacters(1);

            using (var context = new StarWarsContext())
            {
                context.Characters.AddRange(characters);
                context.SaveChanges();
            }

            // TODO: Add character relationships
        }
    }
}
