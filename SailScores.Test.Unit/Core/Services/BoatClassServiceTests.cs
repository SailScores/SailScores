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
    public class BoatClassServiceTests
    {
        private readonly BoatClass _fakeBoatClass;
        private readonly BoatClassService _service;
        private readonly IMapper _mapper;
        private readonly ISailScoresContext _context;


        public BoatClassServiceTests()
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

            _fakeBoatClass = new BoatClass
            {
                Id = Guid.NewGuid(),
                Name = "Fake BoatClass"
            };

            _context.BoatClasses.Add(_mapper.Map<Database.Entities.BoatClass>(_fakeBoatClass));
            _context.SaveChanges();

            _service = new SailScores.Core.Services.BoatClassService(
                _context,
                _mapper
                );


        }

        [Fact]
        public async Task Update_CallsDb()
        {
            var changedClass = new BoatClass
            {
                Id = _fakeBoatClass.Id,
                Name = "New"
            };

            await _service.Update(changedClass);

            Assert.Equal("New", _context.BoatClasses.First().Name);
        }

        [Fact]
        public async Task Delete_RemovesFromDb()
        {

            await _service.Delete(_fakeBoatClass.Id);

            Assert.Empty(_context.BoatClasses);
        }

        [Fact]
        public async Task SaveNew_AddsToDb()
        {
            var changedClass = new BoatClass
            {
                Id = Guid.NewGuid(),
                Name = "New"
            };

            await _service.SaveNew(changedClass);

            Assert.Equal(2, _context.BoatClasses.Count());
        }

        [Fact]
        public async Task GetClass_ReturnsFromDb()
        {


            var result = await _service.GetClass(_fakeBoatClass.Id);

            Assert.Equal(_fakeBoatClass.Name, result.Name);
        }
    }
}
