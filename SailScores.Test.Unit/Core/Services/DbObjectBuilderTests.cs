using AutoMapper;
using SailScores.Core.Services;
using SailScores.Database;
using SailScores.Test.Unit.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using Xunit;

namespace SailScores.Test.Unit.Core.Services
{
    public class DbObjectBuilderTests
    {
        DbObjectBuilder _service;

        private readonly ISailScoresContext _context;
        private readonly IMapper _mapper;

        public DbObjectBuilderTests()
        {
            _context = InMemoryContextBuilder.GetContext();
            _mapper = MapperBuilder.GetSailScoresMapper();
            _service = new DbObjectBuilder(
                _context,
                _mapper);
        }

        [Fact]
        public async Task BuildDbRegatta_Always_AddsSeries()
        {
            // arrange
            var regatta = new Regatta
            {
                Series = new List<Series>
                {
                    new Series
                    {
                        Name = "regattaSeries"
                    }
                }
            };

            // act
            var result = await _service.BuildDbRegattaAsync(regatta);

            // assert
            Assert.NotEmpty(result.RegattaSeries);
        }

        [Fact]
        public async Task BuildDbSeries_Always_AddsRaces()
        {
            // arrange
            var clubId = (await _context.Clubs.FirstAsync()).Id;

            var series = 
                new Series
                {
                    ClubId = clubId,
                Name = "regattaSeries",
                Races = new List<Race>(
                    new List<Race>
                    {
                        new Race
                        {
                            Date = DateTime.Today,
                            Scores = new List<Score>
                            {
                                new Score
                                {
                                    Place = 1,

                                    Competitor = new Competitor
                                    {
                                        ClubId = clubId,
                                        Id =(await _context
                                            .Competitors.FirstAsync()).Id
                                    }
                                }
                            }
                        }
                    })

            };

            // act
            var result = await _service.BuildDbSeriesAsync(series);

            // assert
            Assert.NotEmpty(result.RaceSeries);
        }

        [Fact]
        public async Task BuildDbRaceObj_Always_AddsScores()
        {
            // arrange
            var race =
                new Race
                {
                    Date = DateTime.Today,
                    Scores = new List<Score>
                    {
                        new Score
                        {
                            Place = 1,

                            Competitor = new Competitor
                            {
                                Id =(await _context
                                .Competitors.FirstAsync()).Id
                            }
                        }
                    }
                };

            // act
            var result = await _service.BuildDbRaceObj(
                (await _context.Clubs.FirstAsync()).Id,
                race);

            // assert
            Assert.NotEmpty(result.Scores);
        }
    }
}
