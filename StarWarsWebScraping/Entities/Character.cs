using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarWarsWebScraping.Entities
{
    public class Character
    {
        [Key]
        public int Id { get; set; }
        public string CharacterName { get; set; }
        public string Url { get; set; }

        // TODO: Add Relationships
        //public List<CharacterRelationship> Relationships { get; set; }
    }
}
