using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AutoMapper.QueryableExtensions;
using System.Linq;
using SailScores.Core.Model;

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

        public async Task<IList<Model.Club>> GetClubs(bool includeHidden)
        {
            var dbObjects = await _dbContext
                .Clubs
                .Where(c => includeHidden || !c.IsHidden)
                .ToListAsync();
            return _mapper.Map<List<Model.Club>>(dbObjects);
            //    .ProjectTo<Model.Club>(_mapper.ConfigurationProvider)
            //    .ToListAsync();
            //return returnList;
        }

        public async Task<Model.Club> GetFullClub(string id)
        {
            Guid potentialClubId;
            Club club;
            if(Guid.TryParse(id, out potentialClubId)){
                club = await _dbContext
                    .Clubs
                    .Where(c => c.Id == potentialClubId)
                    .ProjectTo<Model.Club>(_mapper.ConfigurationProvider)
                    .FirstOrDefaultAsync();
            } else
            {
                club = await _dbContext
                    .Clubs
                    .Where(c => c.Initials == id)
                    .ProjectTo<Model.Club>(_mapper.ConfigurationProvider)
                    .FirstOrDefaultAsync();
            }

            return club;
        }
    }
}
