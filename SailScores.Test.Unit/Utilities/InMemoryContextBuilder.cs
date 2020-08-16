using Microsoft.EntityFrameworkCore;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Text;
using SailScores.Database.Entities;

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

            context.Competitors.Add(new Competitor
            {
                Id = Guid.NewGuid(),
                Name = "Comp1",
                BoatName = "Comp1Boat",
                ClubId = club.Id
            });
            context.SaveChanges();

            return context;

        }
    }
}
