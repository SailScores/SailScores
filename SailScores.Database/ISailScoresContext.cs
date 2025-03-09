﻿using Microsoft.EntityFrameworkCore;
using SailScores.Database.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SailScores.Database
{
    public interface ISailScoresContext : IDisposable
    {
        DbSet<Club> Clubs { get; set; }

        DbSet<BoatClass> BoatClasses { get; set; }
        DbSet<Fleet> Fleets { get; set; }
        DbSet<Competitor> Competitors { get; set; }
        DbSet<Season> Seasons { get; set; }
        DbSet<Series> Series { get; set; }
        DbSet<Race> Races { get; set; }
        DbSet<Score> Scores { get; set; }

        DbSet<Regatta> Regattas { get; set; }
        DbSet<Announcement> Announcements { get; set; }

        DbSet<ScoreCode> ScoreCodes { get; set; }

        DbSet<UserClubPermission> UserPermissions { get; set; }

        DbSet<File> Files { get; set; }

        DbSet<Document> Documents { get; set; }
        DbSet<ScoringSystem> ScoringSystems { get; set; }

        DbSet<HistoricalResults> HistoricalResults { get; set; }
        DbSet<SeriesChartResults> SeriesChartResults { get; set; }
        DbSet<SeriesForwarder> SeriesForwarders { get; set; }
        DbSet<RegattaForwarder> RegattaForwarders { get; set; }
        DbSet<CompetitorForwarder> CompetitorForwarders { get; set; }

        DbSet<ClubRequest> ClubRequests { get; set; }
        DbSet<ClubSequence> ClubSequences { get; set; }

        Task<IList<CompetitorStatsSummary>> GetCompetitorStatsSummaryAsync(Guid clubId, Guid competitorId);

        Task<IList<CompetitorRankStats>> GetCompetitorRankCountsAsync(string clubInitials, string sailNumber);
        Task<IList<CompetitorRankStats>> GetCompetitorRankCountsAsync(
            Guid competitorId,
            string seasonUrlName);

        int SaveChanges();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<IList<ClubSeasonStats>> GetClubStats(string clubInitials);
        Task<IList<SiteStats>> GetSiteStats();
        Task<IList<DeletableInfo>> GetDeletableInfoForCompetitorsAsync(Guid clubId);
    }
}
