using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using SailScores.Core.Model;
using Db = SailScores.Database.Entities;
using SailScores.Core.Utility;

namespace SailScores.Core.Services
{
    public class SeasonService : ISeasonService
    {
        private readonly ISailScoresContext _dbContext;
        private readonly IMapper _mapper;

        public SeasonService(
            ISailScoresContext dbContext,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<IList<Season>> GetSeasons(Guid clubId)
        {
            var dbSeasons = _dbContext.Seasons.Where(s => s.ClubId == clubId)
                .OrderByDescending(s => s.Start);
            return _mapper.Map<List<Season>>(
                await dbSeasons.ToListAsync()
                .ConfigureAwait(false));

        }

        public async Task Delete(Guid seasonId)
        {
            var dbSeason = await _dbContext.Seasons.SingleAsync(c => c.Id == seasonId)
                .ConfigureAwait(false);
            _dbContext.Seasons.Remove(dbSeason);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task SaveNew(Season season)
        {
            var dbSeason = _mapper.Map<Db.Season>(season);

            dbSeason.Id = Guid.NewGuid();
            dbSeason.UrlName = UrlUtility.GetUrlName(season.Name);

            if (String.IsNullOrEmpty(dbSeason.UrlName))
            {
                throw new InvalidOperationException("Season name doesn't create a valid url.");
            }
            
            if (_dbContext.Seasons.Any(s =>
                s.ClubId == season.ClubId
                && (s.Name == season.Name || s.UrlName == season.UrlName)))
            {
                throw new InvalidOperationException("Cannot create season. A season with this name already exists.");
            }

            _dbContext.Seasons.Add(dbSeason);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        }

        public async Task Update(Season season)
        {
            if (_dbContext.Seasons.Any(s =>
                s.Id != season.Id
                && s.ClubId == season.ClubId
                && (s.Name == season.Name || s.UrlName == season.UrlName)))
            {
                throw new InvalidOperationException(
                    "Cannot update season. A season with this name already exists.");
            }
            var existingSeason = await _dbContext.Seasons
                .SingleAsync(c => c.Id == season.Id)
                .ConfigureAwait(false);

            if(String.IsNullOrWhiteSpace(existingSeason.UrlName))
            {
                existingSeason.UrlName = UrlUtility.GetUrlName(season.Name);
                if (String.IsNullOrEmpty(existingSeason.UrlName))
                {
                    throw new InvalidOperationException("Season name doesn't create a valid url.");
                }
            }

            existingSeason.Name = season.Name;
            existingSeason.Start = season.Start;
            existingSeason.End = season.End;
            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);
        }

        public async Task<IList<String>> GetSavingSeasonErrors(Season season)
        {
            var errors = new List<String>();
            var existingSeasons = _dbContext.Seasons.Where(s =>
                s.ClubId == season.ClubId);
            if (await existingSeasons.AnyAsync(s =>
                 (s.Name == season.Name || s.UrlName == season.UrlName) && s.Id != season.Id)
                .ConfigureAwait(false))
            {
                errors.Add("A season with this name exists. Please choose a unique name.");
            }
            var overlappingSeason = await existingSeasons.FirstOrDefaultAsync(s => s.Start <= season.End
                    && s.End >= season.Start && s.Id != season.Id)
                .ConfigureAwait(false);
            if (overlappingSeason != null)
            {
                errors.Add($"An existing season ( {overlappingSeason.Name} ) overlaps with this time range.");
            }
            return errors;
        }

        public async Task<IEnumerable<DeletableInfo>> GetDeletableInfo(Guid clubId)
        {
            var seriesSeasons = await _dbContext.Series
                .Where(s => s.ClubId == clubId)
                .Select(s => s.Season.Id).ToListAsync();
            var regattaSeasons = await _dbContext.Regattas
                .Where(s => s.ClubId == clubId)
                .Select(s => s.Season.Id).ToListAsync();
            var info = _dbContext.Seasons
                .Where(s => s.ClubId == clubId)
                .Select(s => new DeletableInfo
                {
                    Id = s.Id,
                    IsDeletable = !seriesSeasons.Any(ss => ss == s.Id)
                        && !regattaSeasons.Any(rs => rs == s.Id),
                    Reason = !seriesSeasons.Any(ss => ss == s.Id)
                        && !regattaSeasons.Any(rs => rs == s.Id) ?
                String.Empty : "Season contains series."
                });
            return info;
        }
    }
}
