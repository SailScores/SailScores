using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using SailScores.Core.Mapping;
using SailScores.Core.Model;
using SailScores.Core.Scoring;
using SailScores.Core.Services;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Unit.Core.Services
{
    public class RegattaServiceTests
    {
        private readonly Series _fakeSeries;
        private readonly Regatta _fakeRegatta;
        private readonly Club _fakeClub;
        private readonly DbObjectBuilder _dbObjectBuilder;
        private readonly RegattaService _service;
        private readonly IMapper _mapper;
        private readonly ISailScoresContext _context;
        private readonly Guid _clubId;
        private readonly Mock<ISeriesService> _mockSeriesService;
        private readonly Season _season;

        public RegattaServiceTests()
        {
            _clubId = Guid.NewGuid();

            _mockSeriesService = new Mock<ISeriesService>();

            var options = new DbContextOptionsBuilder<SailScoresContext>()
                .UseInMemoryDatabase(databaseName: "Series_Test_database")
                .Options;

            _context = new SailScoresContext(options);

            var config = new MapperConfiguration(opts =>
            {
                opts.AddProfile(new DbToModelMappingProfile());
            });

            _mapper = config.CreateMapper();

            var compA = new Competitor
            {
                Name = "Comp A"
            };
            var race1 = new Race
            {
                Date = DateTime.Today
            };

            _season = new Season
            {
                Id = Guid.NewGuid(),
                Name = "New Season",
                Start = new DateTime(2019, 1, 1),
                End = new DateTime(2019, 12, 31)
            };
            _fakeSeries = new Series
            {
                Id = Guid.NewGuid(),
                Name = "Fake Series",
                Competitors = new List<Competitor> {
                    compA
                },
                Races = new List<Race>
                {
                    race1
                },
                Results = new SeriesResults()
            };


            _fakeRegatta = new Regatta
            {
                ClubId = _clubId,
                Season = _season,
                Fleets = new List<Fleet> { new
                    Fleet { FleetType = Api.Enumerations.FleetType.AllBoatsInClub} },
                StartDate = DateTime.Today.AddDays(-3),
                EndDate = DateTime.Today
            };

            _fakeClub = new Club
            {
                Name = "Fake Club",
                Id = _clubId,
                Regattas = new List<Regatta> { _fakeRegatta }
            };

            _context.Series.Add(_mapper.Map<Database.Entities.Series>(_fakeSeries));
            _context.Clubs.Add(_mapper.Map<Database.Entities.Club>(_fakeClub));

            _context.SaveChanges();

            //yep, this means we are testing the real DbObjectBuilder as well:
            _dbObjectBuilder = new DbObjectBuilder(
                _context,
                _mapper
                );
            _service = new SailScores.Core.Services.RegattaService(
                _mockSeriesService.Object,
                _context,
                _dbObjectBuilder,
                _mapper
                );


        }

        [Fact]
        public async Task GetAllRegattas_Always_CallsDb()
        {
            var result = await _service.GetAllRegattasAsync(_clubId);

            Assert.Equal(1, result.Count);
        }


        [Fact]
        public async Task GetRegattasDuringSpan_NoneInSpan_Returns0()
        {
            var result = await _service.GetRegattasDuringSpanAsync(DateTime.Today.AddDays(1), DateTime.Today.AddYears(3));

            Assert.Equal(0, result.Count);
        }

        [Fact]
        public async Task SaveNewRegattaAsync_Null_Throws()
        {
            Exception ex = await Assert.ThrowsAsync<ArgumentNullException>(() => _service.SaveNewRegattaAsync(null));

            Assert.NotNull(ex);
        }

        [Fact]
        public async Task UpdateAsync_Null_Throws()
        {
            Exception ex = await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateAsync(null));

            Assert.NotNull(ex);
        }

        [Fact]
        public async Task AddRaceToRegatta_Null_Throws()
        {
            Exception ex = await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AddRaceToRegattaAsync(null, Guid.NewGuid()));

            Assert.NotNull(ex);
        }

    }
}
