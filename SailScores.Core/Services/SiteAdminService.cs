using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Core.Model;
using SailScores.Core.Model.Summary;
using SailScores.Database;

namespace SailScores.Core.Services;

public class SiteAdminService : ISiteAdminService
{
    private readonly ISailScoresContext _dbContext;
    private readonly IMapper _mapper;

    public SiteAdminService(
        ISailScoresContext dbContext,
        IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<IEnumerable<(ClubSummary club, DateTime? latestSeriesUpdate, DateTime? latestRaceDate)>> GetAllClubsWithDatesAsync()
    {
        var clubs = await _dbContext.Clubs
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Initials,
                c.IsHidden,
                LatestSeriesUpdate = c.Series
                    .OrderByDescending(s => s.UpdatedDate)
                    .Select(s => s.UpdatedDate)
                    .FirstOrDefault(),
                LatestRaceDate = c.Races
                    .OrderByDescending(r => r.Date)
                    .Select(r => r.Date)
                    .FirstOrDefault()
            })
            .OrderBy(c => c.Name)
            .ToListAsync();

        return clubs.Select(c => (
            club: new ClubSummary
            {
                Id = c.Id,
                Name = c.Name,
                Initials = c.Initials,
                IsHidden = c.IsHidden
            },
            latestSeriesUpdate: c.LatestSeriesUpdate,
            latestRaceDate: c.LatestRaceDate
        ));
    }

    public async Task<Club> GetClubDetailsAsync(string clubInitials)
    {
        var dbClub = await _dbContext.Clubs
            .Include(c => c.Series)
                .ThenInclude(s => s.Season)
            .Include(c => c.Races)
            .FirstOrDefaultAsync(c => c.Initials == clubInitials);

        if (dbClub == null)
        {
            return null;
        }

        return _mapper.Map<Club>(dbClub);
    }

    public async Task<Club> GetFullClubForBackupAsync(Guid clubId)
    {
        var dbClub = await _dbContext.Clubs
            .Include(c => c.Fleets)
                .ThenInclude(f => f.FleetBoatClasses)
            .Include(c => c.Competitors)
                .ThenInclude(comp => comp.CompetitorFleets)
            .Include(c => c.BoatClasses)
            .Include(c => c.Seasons)
            .Include(c => c.Series)
                .ThenInclude(s => s.RaceSeries)
            .Include(c => c.Races)
                .ThenInclude(r => r.Scores)
            .Include(c => c.Regattas)
                .ThenInclude(r => r.RegattaSeries)
            .Include(c => c.ScoringSystems)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == clubId);

        if (dbClub == null)
        {
            return null;
        }

        return _mapper.Map<Club>(dbClub);
    }

    public async Task ResetClubAsync(Guid clubId)
    {
        // Delete all races, scores, competitors, series, etc. for the club
        // Keep the club structure (fleets, boat classes, seasons)
        
        // Remove scores for this club's races
        var raceIds = await _dbContext.Races
            .Where(r => r.ClubId == clubId)
            .Select(r => r.Id)
            .ToListAsync();
        
        if (raceIds.Any())
        {
            var scores = await _dbContext.Scores
                .Where(s => raceIds.Contains(s.RaceId))
                .ToListAsync();
            _dbContext.Scores.RemoveRange(scores);
        }

        // Remove races
        var races = await _dbContext.Races
            .Where(r => r.ClubId == clubId)
            .ToListAsync();
        _dbContext.Races.RemoveRange(races);

        // Remove series
        var series = await _dbContext.Series
            .Where(s => s.ClubId == clubId)
            .ToListAsync();
        _dbContext.Series.RemoveRange(series);

        // Remove regattas
        var regattas = await _dbContext.Regattas
            .Where(r => r.ClubId == clubId)
            .ToListAsync();
        _dbContext.Regattas.RemoveRange(regattas);

        // Remove competitors
        var competitors = await _dbContext.Competitors
            .Where(c => c.ClubId == clubId)
            .ToListAsync();
        _dbContext.Competitors.RemoveRange(competitors);

        await _dbContext.SaveChangesAsync();
    }
}
