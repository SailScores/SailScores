using Microsoft.EntityFrameworkCore;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Text;
using SailScores.Database.Entities;
using System.Linq;

namespace SailScores.Test.Unit.Utilities
{
    public class InMemoryContextBuilder
    {
        public static ISailScoresContext GetContext()
        {
            var options = new DbContextOptionsBuilder<SailScoresContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new SailScoresContext(options);

            var club = new Club
            {
                Id = Guid.NewGuid(),
                Name = "Test Club",
                Initials = "TEST",
                Competitors = new List<Competitor>()
            };
            context.Clubs.Add(club);

            var boatClass = new BoatClass
            {
                Id = Guid.NewGuid(),
                Name = "The Boat Class",
                ClubId = club.Id
            };
            context.BoatClasses.Add(boatClass);

            var fleet = new Fleet
            {
                Id = Guid.NewGuid(),
                Name = "The Fleet",
                FleetType = Api.Enumerations.FleetType.AllBoatsInClub,
                ClubId = club.Id
            };
            context.Fleets.Add(fleet);
            var fleet2 = new Fleet
            {
                Id = Guid.NewGuid(),
                Name = "A competitor fleet",
                FleetType = Api.Enumerations.FleetType.SelectedBoats,
                ClubId = club.Id
            };
            context.Fleets.Add(fleet2);


        var competitor = new Competitor
            {
                Id = Guid.NewGuid(),
                Name = "Comp1",
                BoatName = "Comp1Boat",
                ClubId = club.Id,
                BoatClass = boatClass,
                CompetitorFleets = new List<CompetitorFleet>
                {
                    new CompetitorFleet
                    {
                        FleetId = fleet2.Id
                    }
                }
            };
            context.Competitors.Add(competitor);
            
            context.Races.Add(new Race
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Now,
                ClubId = club.Id,
                Scores = new List<Score>
                {
                    new Score
                    {
                        CompetitorId = competitor.Id,
                        Place = 1
                    }
                }
            });

            var scoringSystem = new ScoringSystem
            {
                Id = Guid.NewGuid(),
                Name = "Default for fake club",
                ClubId = club.Id,
                ScoreCodes = new List<ScoreCode>
                {
                    new Database.Entities.ScoreCode
                    {
                        Name = "DNC",
                        CameToStart = false
                    }
                }
            };
            context.ScoringSystems.Add(scoringSystem);
            club.DefaultScoringSystemId = scoringSystem.Id;



            var defaultScoringSystem = new ScoringSystem
            {
                Id = Guid.NewGuid(),
                Name = "Appendix A Low Point For Series",
                ClubId = null,
                ScoreCodes = new List<ScoreCode>
                {
                    new Database.Entities.ScoreCode
                    {
                        Name = "DNC",
                        CameToStart = false
                    }
                }
            };


            context.ScoringSystems.Add(defaultScoringSystem);

            context.SaveChanges();

            return context;

        }
    }
}
