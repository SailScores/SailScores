using AutoMapper;
using SailScores.Core.Services;
using SailScores.Database;
using SailScores.Test.Unit.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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
        public async Task MethodName()
        {

        }
    }
}
