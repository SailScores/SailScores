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
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Initials == clubInitials);

        if (dbClub == null)
        {
            return null;
        }

        return _mapper.Map<Club>(dbClub);
    }
}
