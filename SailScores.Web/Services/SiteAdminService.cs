using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SailScores.Database;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services.Interfaces;
using Newtonsoft.Json;
using AutoMapper;
using SailScores.Core.Model;

namespace SailScores.Web.Services;

public class SiteAdminService : ISiteAdminService
{
    private readonly ISailScoresContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly IMapper _mapper;
    private readonly CoreServices.ISeriesService _seriesService;

    public SiteAdminService(
        ISailScoresContext dbContext,
        IMemoryCache cache,
        IMapper mapper,
        CoreServices.ISeriesService seriesService)
    {
        _dbContext = dbContext;
        _cache = cache;
        _mapper = mapper;
        _seriesService = seriesService;
    }

    public async Task<SiteAdminIndexViewModel> GetAllClubsAsync()
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

        var clubSummaries = clubs.Select(c => new SiteAdminClubSummary
        {
            Id = c.Id,
            Name = c.Name,
            Initials = c.Initials,
            IsHidden = c.IsHidden,
            LatestSeriesUpdate = c.LatestSeriesUpdate,
            LatestRaceDate = c.LatestRaceDate
        }).ToList();

        return new SiteAdminIndexViewModel
        {
            Clubs = clubSummaries
        };
    }

    public async Task<SiteAdminClubDetailsViewModel> GetClubDetailsAsync(string clubInitials)
    {
        var club = await _dbContext.Clubs
            .Include(c => c.Series)
                .ThenInclude(s => s.Season)
            .Include(c => c.Races)
            .FirstOrDefaultAsync(c => c.Initials == clubInitials);

        if (club == null)
        {
            return null;
        }

        var series = _mapper.Map<IList<Series>>(club.Series);

        var vm = new SiteAdminClubDetailsViewModel
        {
            Id = club.Id,
            Name = club.Name,
            Initials = club.Initials,
            IsHidden = club.IsHidden,
            Series = series,
            LatestSeriesUpdate = club.Series
                .OrderByDescending(s => s.UpdatedDate)
                .Select(s => s.UpdatedDate)
                .FirstOrDefault(),
            LatestRaceDate = club.Races
                .OrderByDescending(r => r.Date)
                .Select(r => r.Date)
                .FirstOrDefault(),
            RaceCount = club.Races.Count
        };

        return vm;
    }

    public Task ResetClubInitialsCacheAsync()
    {
        // Note: IMemoryCache doesn't provide a way to enumerate and selectively remove entries.
        // In a production environment, consider using a distributed cache (like Redis) 
        // which supports key pattern matching for selective invalidation.
        // For now, we clear the entire cache as it will be rebuilt on demand.
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0); // Remove all entries
        }
        return Task.CompletedTask;
    }

    public async Task<string> BackupClubAsync(Guid clubId)
    {
        // Get complete club data
        var club = await _dbContext.Clubs
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

        if (club == null)
        {
            return null;
        }

        // Serialize to JSON
        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented
        };

        return JsonConvert.SerializeObject(club, settings);
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

        // Clear cache for this club
        var club = await _dbContext.Clubs.FindAsync(clubId);
        if (club != null)
        {
            _cache.Remove($"ClubId_{club.Initials}");
        }
    }

    public async Task RecalculateSeriesAsync(Guid seriesId, string updatedBy)
    {
        await _seriesService.UpdateSeriesResults(seriesId, updatedBy);
    }
}
