using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Core.Model;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Core.Services
{
    public class SupporterService : ISupporterService
    {
        private readonly ISailScoresContext _dbContext;
        private readonly IMapper _mapper;

        public SupporterService(
            ISailScoresContext dbContext,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<IEnumerable<Supporter>> GetVisibleSupportersAsync()
        {
            var now = DateTime.UtcNow;
            var dbSupporters = await _dbContext.Supporters
                .Where(s => s.IsVisible && (!s.ExpirationDate.HasValue || s.ExpirationDate > now))
                .OrderBy(s => s.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<Supporter>>(dbSupporters);
        }

        public async Task<IEnumerable<Supporter>> GetAllSupportersAsync()
        {
            var dbSupporters = await _dbContext.Supporters
                .OrderBy(s => s.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<Supporter>>(dbSupporters);
        }

        public async Task<Supporter> GetSupporterAsync(Guid id)
        {
            var dbSupporter = await _dbContext.Supporters
                .FirstOrDefaultAsync(s => s.Id == id);

            return _mapper.Map<Supporter>(dbSupporter);
        }

        public async Task SaveNewSupporter(Supporter supporter)
        {
            var dbSupporter = _mapper.Map<Database.Entities.Supporter>(supporter);
            dbSupporter.Id = Guid.NewGuid();

            await _dbContext.Supporters.AddAsync(dbSupporter);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateSupporter(Supporter supporter)
        {
            var dbSupporter = await _dbContext.Supporters
                .FirstOrDefaultAsync(s => s.Id == supporter.Id);

            if (dbSupporter != null)
            {
                _mapper.Map(supporter, dbSupporter);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task DeleteSupporter(Guid id)
        {
            var dbSupporter = await _dbContext.Supporters
                .FirstOrDefaultAsync(s => s.Id == id);

            if (dbSupporter != null)
            {
                dbSupporter.IsVisible = false;
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
