using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace ContosoTeamStats.Data
{
    public class TeamContext : DbContext
    {
        public TeamContext(DbContextOptions<TeamContext> options)
            : base(options)
        {
        }

        public DbSet<Team> Team { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var teams = new List<Team>
            {
                new Team{ID = 1, Name="Adventure Works Cycles"},
                new Team{ID = 2, Name="Alpine Ski House"},
                new Team{ID = 3, Name="Blue Yonder Airlines"},
                new Team{ID = 4, Name="Coho Vineyard"},
                new Team{ID = 5, Name="Contoso, Ltd."},
                new Team{ID = 6, Name="Fabrikam, Inc."},
                new Team{ID = 7, Name="Lucerne Publishing"},
                new Team{ID = 8, Name="Northwind Traders"},
                new Team{ID = 9, Name="Consolidated Messenger"},
                new Team{ID = 10, Name="Fourth Coffee"},
                new Team{ID = 11, Name="Graphic Design Institute"},
                new Team{ID = 12, Name="Nod Publishers"}
            };

            Random r = new();

            foreach (var t in teams)
            {
                t.Wins = r.Next(33);
                t.Losses = r.Next(33);
                t.Ties = r.Next(0, 5);
            }

            modelBuilder.Entity<Team>().HasData(teams);

        }
    }
}
