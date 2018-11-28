using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Sailscores.Database;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AutoMapper.QueryableExtensions;
using System.Linq;
using Sailscores.Core.Model;
using Db = Sailscores.Database.Entities;

namespace Sailscores.Core.Services
{
    public class RaceService : IRaceService
    {
        private readonly ISailscoresContext _dbContext;
        private readonly IMapper _mapper;

        public RaceService(
            ISailscoresContext dbContext,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<IList<Model.Race>> GetRacesAsync(Guid clubId)
        {
            var dbObjects = await _dbContext
                .Clubs
                .Where(c => c.Id == clubId)
                .SelectMany(c => c.Races)
                .ToListAsync();
            return _mapper.Map<List<Model.Race>>(dbObjects);
        }

        public async Task<Model.Race> GetRaceAsync(Guid id)
        {
            var race = await 
                _dbContext
                .Races
                .FirstOrDefaultAsync(c => c.Id == id);

            return _mapper.Map<Model.Race>(race);
        }
    }
}
