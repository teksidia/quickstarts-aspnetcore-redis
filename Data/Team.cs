using ContosoTeamStats.Data;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[ProtoContract]
public class Team
{
    [ProtoMember(1)]
    public int ID { get; set; }

    [ProtoMember(2)]
    public string Name { get; set; }

    [ProtoMember(3)]
    public int Wins { get; set; }

    [ProtoMember(4)]
    public int Losses { get; set; }

    [ProtoMember(5)]
    public int Ties { get; set; }

    static public void PlayGames(IEnumerable<Team> teams)
    {
        // Simple random generation of statistics.
        Random r = new Random();

        foreach (var t in teams)
        {
            t.Wins = r.Next(33);
            t.Losses = r.Next(33);
            t.Ties = r.Next(0, 5);
        }
    }
}

/*public class TeamInitializer : CreateDatabaseIfNotExists<TeamContext>
{
    protected override void Seed(TeamContext context)
    {
        var teams = new List<Team>
        {
            new Team{Name="Adventure Works Cycles"},
            new Team{Name="Alpine Ski House"},
            new Team{Name="Blue Yonder Airlines"},
            new Team{Name="Coho Vineyard"},
            new Team{Name="Contoso, Ltd."},
            new Team{Name="Fabrikam, Inc."},
            new Team{Name="Lucerne Publishing"},
            new Team{Name="Northwind Traders"},
            new Team{Name="Consolidated Messenger"},
            new Team{Name="Fourth Coffee"},
            new Team{Name="Graphic Design Institute"},
            new Team{Name="Nod Publishers"}
        };

        Team.PlayGames(teams);

        teams.ForEach(t => context.Teams.Add(t));
        context.SaveChanges();
    }
}

public class TeamConfiguration : DbConfiguration
{
    public TeamConfiguration()
    {
        SetExecutionStrategy("System.Data.SqlClient", () => new SqlAzureExecutionStrategy());
    }
}*/