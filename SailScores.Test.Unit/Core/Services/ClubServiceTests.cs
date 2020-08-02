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
using SailScores.Api.Dtos;
using Xunit;

namespace SailScores.Test.Unit.Core.Services
{
    public class ClubServiceTests
    {
        private readonly ClubService _service;
        private readonly Mock<IScoringCalculator> _mockCalculator;
        private readonly Mock<IScoringCalculatorFactory> _mockScoringCalculatorFactory;
        private readonly IMapper _mapper;
        private readonly ISailScoresContext _context;

        public ClubServiceTests()
        {
            var options = new DbContextOptionsBuilder<SailScoresContext>()
                .UseInMemoryDatabase(databaseName: "Series_Test_database")
                .Options;

            _context = new SailScoresContext(options);

            var config = new MapperConfiguration(opts =>
            {
                opts.AddProfile(new DbToModelMappingProfile());
            });

            _mapper = config.CreateMapper();
            _context.SaveChanges();

            _service = new SailScores.Core.Services.ClubService(
                _context,
                _mapper
                );
        }

    }
}
