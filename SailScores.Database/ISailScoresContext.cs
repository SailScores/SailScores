using Microsoft.EntityFrameworkCore;
using SailScores.Database.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SailScores.Database
{
    public interface ISailScoresContext : IDisposable {
        DbSet<Club> Clubs { get; set; }

        DbSet<BoatClass> BoatClasses { get; set; }
        DbSet<Fleet> Fleets { get; set; }
        DbSet<Competitor> Competitors { get; set; }
        DbSet<Season> Seasons { get; set; }
        DbSet<Series> Series { get; set; }
        DbSet<Race> Races { get; set; }
        DbSet<Score> Scores { get; set; }

        DbSet<Regatta> Regattas { get; set; }

        DbSet<ScoreCode> ScoreCodes { get; set; }

        DbSet<UserClubPermission> UserPermissions { get; set; }

        DbSet<File> Files { get; set; }
        DbSet<ScoringSystem> ScoringSystems { get; set; }

        DbSet<HistoricalResults> HistoricalResults { get; set; }
        DbSet<SeriesChartResults> SeriesChartResults { get; set; }

        int SaveChanges();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));

    }
}
