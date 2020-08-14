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
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Unit.Core.Services
{
    public class ClubRequestServiceTests
    {
        private readonly ClubRequestService _service;
        private readonly IMapper _mapper;
        private ISailScoresContext _context;
        private ClubRequest _fakeInProcessClubRequest;
        private ClubRequest _fakeCompletedClubRequest;


        public ClubRequestServiceTests()
        {


            var options = new DbContextOptionsBuilder<SailScoresContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new SailScoresContext(options);

            var config = new MapperConfiguration(opts =>
            {
                opts.AddProfile(new DbToModelMappingProfile());
            });

            _mapper = config.CreateMapper();

            _fakeInProcessClubRequest = new ClubRequest()
            {
                Id = Guid.NewGuid(),
                ClubName = "InProcessClubRequestName"
            };

            _fakeCompletedClubRequest = new ClubRequest()
            {
                Id = Guid.NewGuid(),
                ClubName = "CompletedClubRequestName",
                Complete = true
            };

            _context.ClubRequests.Add(_mapper.Map<Database.Entities.ClubRequest>(
                _fakeInProcessClubRequest));
            _context.ClubRequests.Add(_mapper.Map<Database.Entities.ClubRequest>(
                _fakeCompletedClubRequest));
            _context.SaveChanges();

            _context = new SailScoresContext(options);

            _service = new SailScores.Core.Services.ClubRequestService(
                _context,
                _mapper
                );

        }

        [Fact]
        public async Task GetPendingRequests_ReturnsOnlyPending()
        {
            var result = await _service.GetPendingRequests();

            Assert.Equal(1, result.Count);
        }

        [Fact]
        public async Task GetRequest_ReturnsCorrect()
        {
            var result = await _service.GetRequest(_fakeCompletedClubRequest.Id);

            Assert.Equal(result.ClubName, _fakeCompletedClubRequest.ClubName);
        }

        [Fact]
        public async Task Submit_AddsRequest()
        {
            var newCompletedClubRequest = new ClubRequest()
            {
                Id = Guid.NewGuid(),
                ClubName = "NewCompletedClubRequestName",
                Complete = true
            };

            await _service.Submit(newCompletedClubRequest);

            Assert.Equal(3, _context.ClubRequests.Count());
        }

        [Fact]
        public async Task Update_ChangesRequest()
        {
            var changedClubRequest = new ClubRequest()
            {
                Id = _fakeCompletedClubRequest.Id,
                ClubName = "NewName",
                Complete = false
            };

            await _service.UpdateRequest(changedClubRequest);

            Assert.Equal(2, _context.ClubRequests.Count());
            Assert.Equal("NewName",
                _context.ClubRequests.FirstOrDefault(c =>
                    c.Id == _fakeCompletedClubRequest.Id).ClubName);
        }
    }
}
