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
                Name = "Test Club"
            };
            context.Clubs.Add(club);

            var boatClass = new BoatClass
            {
                Id = Guid.NewGuid(),
                Name = "The Boat Class",
                ClubId = club.Id
            };
            context.BoatClasses.Add(boatClass);

            var competitor = new Competitor
            {
                Id = Guid.NewGuid(),
                Name = "Comp1",
                BoatName = "Comp1Boat",
                ClubId = club.Id,
                BoatClass = boatClass
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

            context.SaveChanges();

            return context;

        }
    }
}
