using Microsoft.EntityFrameworkCore;
using StarWarsWebScraping.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarWarsWebScraping
{
    public class StarWarsContext : DbContext
    {
        public DbSet<Character> Characters { get; set; }
        public DbSet<CharacterRelationship> Relationships { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=localhost;Database=StarWarsStuff;Trusted_Connection=True;");
        }

        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    base.OnModelCreating(modelBuilder);
        //}
    }
}
