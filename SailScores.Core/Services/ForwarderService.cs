using Microsoft.EntityFrameworkCore;
using SailScores.Database;
using System;
using System.Threading.Tasks;
using SailScores.Core.Model.Forwarder;
using SailScores.Database.Migrations;
using System.Collections.Generic;
using SailScores.Database.Entities;
using Competitor = SailScores.Core.Model.Competitor;
using System.ComponentModel.Design;
using System.Linq;

namespace SailScores.Core.Services;

/// <summary>
/// Forwarder service should be used when a url fails to find a matching
/// series, regatta, or competitor.  It is responsible for looking in a
/// list of historical urls to see if there is a match. If one is found,
/// it should return an object that will be used by the controller to
/// redirect to the correct url.
/// </summary>

public class ForwarderService : IForwarderService
{
    private readonly ISailScoresContext _dbContext;

    public ForwarderService(
        ISailScoresContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SeriesForwarderResult> GetSeriesForwarding(
        string clubInitials,
        string seasonUrlName,
        string seriesUrlName)
    {
        var result = await _dbContext.SeriesForwarders
            .Include(sf => sf.NewSeries)
            .ThenInclude(s => s.Season)
            .FirstOrDefaultAsync(sf =>
                sf.OldClubInitials == clubInitials
                && sf.OldSeasonUrlName == seasonUrlName
                && sf.OldSeriesUrlName == seriesUrlName);
        var club = await _dbContext.Clubs.FirstOrDefaultAsync(c => c.Initials == clubInitials);
        // key to avoiding forwarding loops: only return if there is a change.
        if (result != null &&
            !String.IsNullOrEmpty(result.NewSeries?.Season?.UrlName) &&
            !String.IsNullOrEmpty(result.NewSeries?.UrlName) &&
           (!seasonUrlName.Equals(result.NewSeries.Season.UrlName, StringComparison.CurrentCultureIgnoreCase) ||
            !seriesUrlName.Equals(result.NewSeries.UrlName, StringComparison.CurrentCultureIgnoreCase) ||
            !clubInitials.Equals(club.Initials, StringComparison.CurrentCultureIgnoreCase)))
        {
            return new SeriesForwarderResult
            {
                OldClubInitials = clubInitials,
                OldSeasonUrlName = seasonUrlName,
                OldSeriesUrlName = seriesUrlName,
                NewClubInitials = club.Initials,
                NewSeasonUrlName = result.NewSeries.Season.UrlName,
                NewSeriesUrlName = result.NewSeries.UrlName
            };
        }
        return null;
    }

    public async Task<RegattaForwarderResult> GetRegattaForwarding(string clubInitials, string seasonUrlName, string regattaUrlName)
    {
        var result = await _dbContext.RegattaForwarders
            .Include(rf => rf.NewRegatta)
            .ThenInclude(r => r.Season)
            .FirstOrDefaultAsync(rf =>
                           rf.OldClubInitials == clubInitials &&
                           rf.OldSeasonUrlName == seasonUrlName &&
                           rf.OldRegattaUrlName == regattaUrlName);
    
        var club = await _dbContext.Clubs.FirstOrDefaultAsync(c => c.Initials == clubInitials);
        // key to avoiding forwarding loops: only return if there is a change.
        if (result != null &&
            !String.IsNullOrEmpty(result.NewRegatta?.Season?.UrlName) &&
            !String.IsNullOrEmpty(result.NewRegatta?.UrlName) &&
           (!seasonUrlName.Equals(result.NewRegatta.Season.UrlName, StringComparison.CurrentCultureIgnoreCase) ||
            !regattaUrlName.Equals(result.NewRegatta.UrlName, StringComparison.CurrentCultureIgnoreCase) ||
            !clubInitials.Equals(club.Initials, StringComparison.CurrentCultureIgnoreCase)))
        {
            return new RegattaForwarderResult
            {
                OldClubInitials = clubInitials,
                OldSeasonUrlName = seasonUrlName,
                OldRegattaUrlName = regattaUrlName,
                NewClubInitials = club.Initials,
                NewSeasonUrlName = result.NewRegatta.Season.UrlName,
                NewRegattaUrlName = result.NewRegatta.UrlName
            };
        }
        return null;
    }

    public async Task<CompetitorForwarderResult> GetCompetitorForwarding(string clubInitials, string urlName)
    {

        var result = await _dbContext.CompetitorForwarders
            .Include(rf => rf.NewCompetitor)
            .FirstOrDefaultAsync(rf =>
                   rf.OldClubInitials == clubInitials &&
                   rf.OldCompetitorUrl == urlName);

        if (result == null)
        {
            return null;
        }

        var club = await _dbContext.Clubs.FirstOrDefaultAsync(c => c.Initials == clubInitials);

        if (club.Initials.Equals(clubInitials) &&
            (urlName.Equals(result.NewCompetitor.UrlName, StringComparison.CurrentCultureIgnoreCase)))
        {
            // matches, looks like a loop: get out of it:
            return null;
        }
        var competitorUrlToUse = UrlUtility.GetUrlName(result.NewCompetitor.UrlName ?? result.NewCompetitor.SailNumber);

        return new CompetitorForwarderResult
        {
            OldClubInitials = clubInitials,
            OldUrlName = UrlUtility.GetUrlName(urlName),
            NewClubInitials = club.Initials,
            NewUrlName = competitorUrlToUse
        };
    }

    public async Task CreateSeriesForwarder(Model.Series newSeries, Series oldSeries)
    {
        // fill in clubInitials: for now only have new club initials: we won't allow movement between clubs.
        var clubInitials = (await _dbContext.Clubs.SingleAsync(c => c.Id == oldSeries.ClubId)).Initials;

        // build possible forwarder:
        var potentialForwarder = new SeriesForwarder
        {
            Id = Guid.NewGuid(),
            OldClubInitials = clubInitials,
            OldSeasonUrlName = oldSeries.Season.UrlName,
            OldSeriesUrlName = oldSeries.UrlName,
            NewSeriesId = oldSeries.Id,
            Created = DateTime.UtcNow
        };

        bool saveForwarder = true;
        // check the potential forwarder for conflicts with existing forwarders
        var matches = _dbContext.SeriesForwarders.Where(sf =>
            sf.OldClubInitials == potentialForwarder.OldClubInitials &&
            sf.OldSeasonUrlName == potentialForwarder.OldSeasonUrlName &&
            sf.OldSeriesUrlName == potentialForwarder.OldSeriesUrlName);
        foreach (var match in matches.ToList())
        {
            if (match.NewSeriesId == potentialForwarder.NewSeriesId)
            {
                saveForwarder = false;
                break;
            }
            else // id doesn't match, it's pointing at a different regatta. Last one in wins:
            {
                _dbContext.SeriesForwarders.Remove(match);
            }
        }

        if (saveForwarder)
        {
            _dbContext.SeriesForwarders.Add(potentialForwarder);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task CreateRegattaForwarder(Model.Regatta newRegatta, Regatta oldRegatta)
    {
        // fill in clubInitials: for now only have new club initials: we won't allow movement between clubs.
        var clubInitials = (await _dbContext.Clubs.SingleAsync(c => c.Id == oldRegatta.ClubId)).Initials;

        // build possible forwarder:
        var potentialForwarder = new RegattaForwarder
        {
            Id = Guid.NewGuid(),
            OldClubInitials = clubInitials,
            OldSeasonUrlName = oldRegatta.Season.UrlName,
            OldRegattaUrlName = oldRegatta.UrlName,
            NewRegattaId = oldRegatta.Id,
            Created = DateTime.UtcNow
        };

        bool saveForwarder = true;
        // check the potential forwarder for conflicts with existing forwarders
        var matches = _dbContext.RegattaForwarders.Where(rf =>
            rf.OldClubInitials == potentialForwarder.OldClubInitials &&
            rf.OldSeasonUrlName == potentialForwarder.OldSeasonUrlName &&
            rf.OldRegattaUrlName == potentialForwarder.OldRegattaUrlName);
        foreach (var match in matches.ToList())
        {
            if(match.NewRegattaId == potentialForwarder.NewRegattaId)
            {
                saveForwarder = false;
                break;
            } else // id doesn't match, it's pointing at a different regatta. Last one in wins:
            {
                _dbContext.RegattaForwarders.Remove(match);
            }
        }

        if (saveForwarder)
        {
            _dbContext.RegattaForwarders.Add(potentialForwarder);
            await _dbContext.SaveChangesAsync();
        }

    }

    public async Task CreateCompetitorForwarder(Competitor newCompetitor, Database.Entities.Competitor oldCompetitor)
    {
        if (newCompetitor == null || oldCompetitor == null)
        {
            return;
        }
        if (newCompetitor.UrlName == oldCompetitor.UrlName)
        {
            return;
        }

        // first build possible forwarders:
        var forwarder = new CompetitorForwarder{
                Id = Guid.NewGuid(),
                OldCompetitorUrl = oldCompetitor.UrlName,
                CompetitorId = oldCompetitor.Id,
                Created = DateTime.UtcNow
            };

        // fill in clubInitials: for now only have new club initials: we won't allow movement between clubs.
        var clubInitials = (await _dbContext.Clubs.SingleAsync(c => c.Id == oldCompetitor.ClubId)).Initials;

        // check the potential forwarder for conflicts with existing forwarders
        
        forwarder.OldClubInitials = clubInitials;
        var matches = _dbContext.CompetitorForwarders.Where(cf =>
            cf.OldClubInitials == forwarder.OldClubInitials &&
            cf.OldCompetitorUrl == forwarder.OldCompetitorUrl);

        // really expect max of one, but handle more.
        foreach(var match in matches.ToList())
        {
            if (match.CompetitorId == forwarder.CompetitorId)
            {
                // forwarder not needed.
                // exit this method.
                return;
            }

            if (match.CompetitorId != forwarder.CompetitorId)
            {
                _dbContext.CompetitorForwarders.Remove(match);
            }
        }
        _dbContext.CompetitorForwarders.Add(forwarder);
        await _dbContext.SaveChangesAsync();
    }

}
