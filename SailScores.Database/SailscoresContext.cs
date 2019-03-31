using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SailScores.Api.Enumerations;
using SailScores.Database.Entities;

namespace SailScores.Database
{
    public class SailScoresContext : DbContext, ISailScoresContext
    {
        public DbSet<Club> Clubs { get; set; }
        public DbSet<Fleet> Fleets { get; set; }
        public DbSet<BoatClass> BoatClasses { get; set; }
        public DbSet<Competitor> Competitors { get; set; }
        public DbSet<Season> Seasons { get; set; }
        public DbSet<Series> Series { get; set; }
        public DbSet<Race> Races { get; set; }
        public DbSet<Score> Scores { get; set; }

        public DbSet<ScoreCode> ScoreCodes { get; set; }

        public DbSet<ScoringSystem> ScoringSystems { get; set; }

        public DbSet<UserClubPermission> UserPermissions { get; set; }

        public DbSet<File> Files { get; set; }

        public DbSet<HistoricalResults> HistoricalResults { get; set; }

        public SailScoresContext(
            DbContextOptions<SailScoresContext> options)
            : base(options)
        {
        }

        protected override void
            OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CompetitorFleet>()
                .HasKey(x => new {x.CompetitorId, x.FleetId});
            modelBuilder.Entity<SeriesRace>()
                .HasKey(x => new {x.SeriesId, x.RaceId});
            modelBuilder.Entity<FleetBoatClass>()
                .HasKey(x => new {x.FleetId, x.BoatClassId});


            // Following lines resolve multiple deletion paths to entities.
            modelBuilder.Entity<Club>()
                .HasMany(c => c.Competitors)
                .WithOne(c => c.Club)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Series>()
                .HasMany(s => s.RaceSeries)
                .WithOne(rs => rs.Series)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Series>()
                .HasOne(s => s.Season)
                .WithMany(s => s.Series)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Fleet>()
                .HasMany(f => f.CompetitorFleets)
                .WithOne(cf => cf.Fleet)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Fleet>()
                .HasMany(f => f.FleetBoatClasses)
                .WithOne(c => c.Fleet)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Score>()
                .HasOne(s => s.Competitor)
                .WithMany(c => c.Scores)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HistoricalResults>()
                .Property(b => b.Created)
                .HasDefaultValueSql("getdate()");

            modelBuilder
                .Entity<Race>()
                .Property(e => e.State)
                .HasConversion(
                    v => v.ToString(),
                    v => (RaceState)Enum.Parse(typeof(RaceState), v));

        }

        public override int SaveChanges()
        {
            return base.SaveChanges();
        }
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
