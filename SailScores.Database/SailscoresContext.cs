using System;
using System.Collections.Generic;
using System.Linq;
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
        public DbSet<Regatta> Regattas { get; set; }

        public DbSet<ScoreCode> ScoreCodes { get; set; }

        public DbSet<ScoringSystem> ScoringSystems { get; set; }

        public DbSet<UserClubPermission> UserPermissions { get; set; }

        public DbSet<File> Files { get; set; }

        public DbSet<HistoricalResults> HistoricalResults { get; set; }
        public DbSet<SeriesChartResults> SeriesChartResults { get; set; }

        public DbSet<ClubRequest> ClubRequests { get; set; }

        public SailScoresContext(
            DbContextOptions<SailScoresContext> options)
            : base(options)
        {
        }

        protected override void
            OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                // EF Core 1 & 2
                //property.Relational().ColumnType = "decimal(18, 2)";

                // EF Core 3
                property.SetColumnType("decimal(18, 2)");
            }


            modelBuilder.Entity<CompetitorFleet>()
                .HasKey(x => new {x.CompetitorId, x.FleetId});
            modelBuilder.Entity<SeriesRace>()
                .HasKey(x => new {x.SeriesId, x.RaceId});
            modelBuilder.Entity<FleetBoatClass>()
                .HasKey(x => new {x.FleetId, x.BoatClassId});
            modelBuilder.Entity<RegattaFleet>()
                .HasKey(x => new { x.RegattaId, x.FleetId });
            modelBuilder.Entity<RegattaSeries>()
                .HasKey(x => new { x.RegattaId, x.SeriesId });

            modelBuilder.Entity<Club>()
                .HasOne(c => c.DefaultScoringSystem)
                .WithMany(s => s.DefaultForClubs)
                .HasForeignKey(c => c.DefaultScoringSystemId);

            // Following lines resolve multiple deletion paths to entities.
            modelBuilder.Entity<Club>()
                .HasMany(c => c.Competitors);

            modelBuilder.Entity<Series>()
                .HasMany(s => s.RaceSeries)
                .WithOne(rs => rs.Series)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Series>()
                .HasOne(s => s.Season)
                .WithMany(s => s.Series)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Series>()
                .Property(e => e.UpdatedDate)
                .HasConversion(v => v, v =>
                    v.HasValue ?
                    DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) :
                    v);

            modelBuilder.Entity<Regatta>()
                .Property(e => e.UpdatedDate)
                .HasConversion(v => v, v =>
                    v.HasValue ?
                    DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) :
                    v);

            modelBuilder.Entity<Fleet>()
                .HasMany(f => f.CompetitorFleets)
                .WithOne(cf => cf.Fleet)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Fleet>()
                .HasMany(f => f.FleetBoatClasses)
                .WithOne(c => c.Fleet)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Regatta>()
                .HasMany(f => f.RegattaFleet)
                .WithOne(c => c.Regatta)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Regatta>()
                .HasMany(f => f.RegattaSeries)
                .WithOne(c => c.Regatta)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Regatta>()
                .HasOne(f => f.Season)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Score>()
                .HasOne(s => s.Competitor)
                .WithMany(c => c.Scores)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HistoricalResults>()
                .Property(b => b.Created)
                .HasDefaultValueSql("getdate()");

            modelBuilder.Entity<SeriesChartResults>()
                .Property(b => b.Created)
                .HasDefaultValueSql("getdate()");

            modelBuilder
                .Entity<Race>()
                .Property(e => e.State)
                .HasConversion(
                    v => v.ToString(),
                    v => (RaceState)Enum.Parse(typeof(RaceState), v));

            modelBuilder
                .Entity<Series>()
                .Property(e => e.TrendOption)
                .HasConversion(
                    v => v.ToString(),
                    v => (TrendOption)Enum.Parse(typeof(TrendOption), v));



        }

        public override int SaveChanges()
        {
            return base.SaveChanges();
        }
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
