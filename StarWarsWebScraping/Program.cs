namespace StarWarsWebScraping
{
    class Program
    {

        static void Main(string[] args)
        {
            // Change this to get all characters then get their relationships.
            var scraper = new Scraper();

            var characters = scraper.GetAllCharacters();

            using (var context = new StarWarsContext())
            {
                context.AddRange(characters);
                //context.AddRange(Characters.Characters);
                //context.AddRange(Characters.Relationships);
                context.SaveChanges();
            }
        }
    }
}
