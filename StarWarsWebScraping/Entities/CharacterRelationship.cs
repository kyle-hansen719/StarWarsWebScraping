using System.ComponentModel.DataAnnotations;

namespace StarWarsWebScraping.Entities
{
    public class CharacterRelationship
    {
        [Key]
        public int Id { get; set; }

        public int CharacterId { get; set; }
        public Character Character { get; set; }

        public int? HyperlinkCharacterId { get; set; }
        public Character HyperlinkCharacter { get; set; }
    }
}
