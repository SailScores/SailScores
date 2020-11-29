using Microsoft.EntityFrameworkCore;
using SailScores.Database;
using System;
using System.Collections.Generic;
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

            var season = new Season
            {
                Id = Guid.NewGuid(),
                Name = "Test Season",
                Start = new DateTime(2020, 1, 1),
                End = new DateTime(2021, 1, 1),
                UrlName = "TestSeason"
            };

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
            
            var inactiveCompetitor = new Competitor
            {
                Id = Guid.NewGuid(),
                Name = "Comp12",
                BoatName = "Comp12Boat",
                ClubId = club.Id,
                BoatClass = boatClass,
                CompetitorFleets = new List<CompetitorFleet>
                {
                    new CompetitorFleet
                    {
                        FleetId = fleet2.Id
                    }
                },
                IsActive = false
            };
            context.Competitors.Add(inactiveCompetitor);




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

            var series = new Series
            {
                Name = "Series One",
                UrlName = "SeriesOne",
                ClubId = club.Id,
                Season = season
            };
            context.Series.Add(series);

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
                },
                SeriesRaces = new List<SeriesRace> {
                    new SeriesRace
                    {
                        Series = series
                    }
                }
            });

            var regattaSeries = new Series
            {
                Id = Guid.NewGuid(),
                Name = "Regatta Series",
                UrlName = "RegattaurlName"
            };
            var regattaFleet = new
                Fleet
                {FleetType = Api.Enumerations.FleetType.AllBoatsInClub};
            var regatta = new Regatta
            {
                ClubId = club.Id,
                Season = season,
                RegattaFleet = new List<RegattaFleet>
                { new RegattaFleet
                    {
                        Fleet = regattaFleet
                    }
                },
                StartDate = season.Start.AddMonths(6),
                EndDate = season.Start.AddMonths(6).AddDays(3),
                RegattaSeries = new List<RegattaSeries>
                {
                    new RegattaSeries
                    {
                        Series = regattaSeries
                    }
                }
            };
            context.Regattas.Add(regatta);

            context.SaveChanges();

            return context;

        }
    }
}
