using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AutoMapper.QueryableExtensions;

namespace SailScores.Core.Services
{
    public class ClubService : IClubService
    {
        private readonly ISailScoresContext _dbContext;
        private readonly IMapper _mapper;

        public ClubService(
            ISailScoresContext dbContext,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<IList<Model.Club>> GetClubs()
        {
            var returnList = await _dbContext
                .Clubs
                .ProjectTo<Model.Club>(_mapper.ConfigurationProvider)
                .ToListAsync();
            return returnList;
        }
    }
}
