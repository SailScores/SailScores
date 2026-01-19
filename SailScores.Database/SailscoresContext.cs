using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SailScores.Api.Enumerations;
using SailScores.Database.Entities;
using File = SailScores.Database.Entities.File;

namespace SailScores.Database;

public class SailScoresContext : DbContext, ISailScoresContext
{
    private const string ClubIdParameterName = "ClubId";
    private readonly Assembly _executingAssembly;

    public DbSet<Club> Clubs { get; set; }
    public DbSet<ClubSequence> ClubSequences { get; set; }
    public DbSet<Fleet> Fleets { get; set; }
    public DbSet<BoatClass> BoatClasses { get; set; }
    public DbSet<Competitor> Competitors { get; set; }
    public DbSet<Season> Seasons { get; set; }
    public DbSet<Series> Series { get; set; }
    public DbSet<Race> Races { get; set; }
    public DbSet<Score> Scores { get; set; }
    public DbSet<Regatta> Regattas { get; set; }
    public DbSet<Announcement> Announcements { get; set; }

    public DbSet<ScoreCode> ScoreCodes { get; set; }

    public DbSet<ScoringSystem> ScoringSystems { get; set; }

    public DbSet<UserClubPermission> UserPermissions { get; set; }

    public DbSet<File> Files { get; set; }
    public DbSet<Document> Documents { get; set; }

    public DbSet<HistoricalResults> HistoricalResults { get; set; }
    public DbSet<SeriesChartResults> SeriesChartResults { get; set; }
    public DbSet<SeriesForwarder> SeriesForwarders { get; set; }
    public DbSet<RegattaForwarder> RegattaForwarders { get; set; }
    public DbSet<CompetitorForwarder> CompetitorForwarders { get; set; }
    public DbSet<SeriesToSeriesLink> SeriesToSeriesLinks { get; set; }

    public DbSet<ClubRequest> ClubRequests { get; set; }

    public DbSet<SystemAlert> SystemAlerts { get; set; }
    public DbSet<Supporter> Supporters { get; set; }

    // Junction tables for relationships
    public DbSet<SeriesRace> SeriesRaces { get; set; }
    public DbSet<CompetitorFleet> CompetitorFleets { get; set; }
    public DbSet<FleetBoatClass> FleetBoatClasses { get; set; }
    public DbSet<RegattaSeries> RegattaSeries { get; set; }
    public DbSet<RegattaFleet> RegattaFleets { get; set; }
    public DbSet<SeriesToSeriesLink> SeriesToSeriesLinks { get; set; }

    // these sets below are not tables in the database 
    private DbSet<CompetitorStatsSummary> CompetitorStatsSummary { get; set; }
    private DbSet<CompetitorRankStats> CompetitorRankStats { get; set; }
    private DbSet<ClubSeasonStats> ClubSeasonStats { get; set; }
    private DbSet<SiteStats> SiteStats { get; set; }
    private DbSet<DeletableInfo> CompetitorDeletableInfo { get; set; }
    private DbSet<CompetitorActiveDates> CompetitorActiveDates { get; set; }

    private DbSet<AllCompHistogramFields> AllCompHistogramFields { get; set; }
    private DbSet<AllCompHistogramStats> AllCompHistogramStats { get; set; }

    public DbSet<ChangeType> ChangeTypes { get; set; }
    public DbSet<CompetitorChange> CompetitorChanges { get; set; }

    public async Task<IList<AllCompHistogramFields>> GetAllCompHistogramFields(
        Guid clubId,
        DateTime? startDate,
        DateTime? endDate)
    {
        var query = await GetSqlQuery("AllCompHistogramFields");
        var clubParam = new SqlParameter(ClubIdParameterName, clubId);
        var startDateParam = new SqlParameter("StartDate", startDate ?? (object)DBNull.Value);
        var endDateParam = new SqlParameter("EndDate", endDate ?? (object)DBNull.Value);
        var result = await this.AllCompHistogramFields
            .FromSqlRaw(query, clubParam, startDateParam, endDateParam)
            .ToListAsync();
        return result;
    }

    public async Task<IList<AllCompHistogramStats>> GetAllCompHistogramStats(
        Guid clubId,
        DateTime? startDate,
        DateTime? endDate)
    {
        var query = await GetSqlQuery("AllCompHistogram");
        var clubParam = new SqlParameter(ClubIdParameterName, clubId);
        var startDateParam = new SqlParameter("StartDate", startDate ?? (object)DBNull.Value);
        var endDateParam = new SqlParameter("EndDate", endDate ?? (object)DBNull.Value);
        var result = await this.AllCompHistogramStats
            .FromSqlRaw(query, clubParam, startDateParam, endDateParam)
            .ToListAsync();
        return result;
    }

    public async Task<IList<CompetitorStatsSummary>> GetCompetitorStatsSummaryAsync(Guid clubId, Guid competitorId)
    {
        var query = "EXECUTE dbo.SS_SP_GetSeasonSummary @CompetitorId = @competitorId, @ClubId = @clubId";

        var clubParam = new SqlParameter("clubId", clubId);
        var sailParam = new SqlParameter("competitorId", competitorId);
        var result = await this.CompetitorStatsSummary
            .FromSqlRaw(query, sailParam, clubParam)
            .ToListAsync();
        return result;
    }

    public async Task<IList<CompetitorRankStats>> GetCompetitorRankCountsAsync(string clubInitials, string sailNumber)
    {
        var query = await GetSqlQuery("RankCounts");
        var clubParam = new SqlParameter("ClubInitials", clubInitials);
        var sailParam = new SqlParameter("SailNumber", sailNumber);
        var result = await this.CompetitorRankStats
            .FromSqlRaw(query, clubParam, sailParam)
            .ToListAsync();
        return result;
    }

    public async Task<IList<CompetitorRankStats>> GetCompetitorRankCountsAsync(
        Guid competitorId,
        string seasonUrlName)
    {
        var query = await GetSqlQuery("RankCountsById");
        var competitorParam = new SqlParameter("CompetitorId", competitorId);
        var seasonParam = new SqlParameter("SeasonUrlName", seasonUrlName);
        var result = await this.CompetitorRankStats
            .FromSqlRaw(query, competitorParam, seasonParam)
            .ToListAsync();
        return result;
    }

    public async Task<IList<ClubSeasonStats>> GetClubStats(
        string clubInitials)
    {
        var query = await GetSqlQuery("ClubStats");
        var clubParam = new SqlParameter("ClubInitials", clubInitials);
        var result = await this.ClubSeasonStats
            .FromSqlRaw(query, clubParam)
            .ToListAsync();
        return result;
    }

    public async Task<IList<SiteStats>> GetSiteStats()
    {
        var query = await GetSqlQuery("SiteStats");
        var result = await this.SiteStats
            .FromSqlRaw(query)
            .ToListAsync();
        return result;
    }


    public async Task<IList<DeletableInfo>> GetDeletableInfoForCompetitorsAsync(Guid clubId)
    {
        var query = await GetSqlQuery("DeletableCompetitors");
        var clubParam = new SqlParameter(ClubIdParameterName, clubId); 
        var result = await this.CompetitorDeletableInfo
            .FromSqlRaw(query, clubParam)
            .ToListAsync();
        return result;
    }

    public async Task<IList<CompetitorActiveDates>> GetCompetitorActiveDates(Guid clubId)
    {
        var query = await GetSqlQuery("CompetitorActiveDates");
        var clubParam = new SqlParameter(ClubIdParameterName, clubId);
        var result = await this.CompetitorActiveDates
            .FromSqlRaw(query, clubParam)
            .ToListAsync();
        return result;
    }


    private async Task<string> GetSqlQuery(string name)
    {
        string resourceName = $"SailScores.Database.Sql.{name}.sql";
        using (Stream stream = _executingAssembly.GetManifestResourceStream(resourceName))
        using (StreamReader reader = new StreamReader(stream))
        {
            return await reader.ReadToEndAsync();
        }
    }

    public SailScoresContext(
        DbContextOptions<SailScoresContext> options)
        : base(options)
    {
        _executingAssembly = typeof(SailScoresContext).Assembly;
    }

    protected override void
        OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            // EF Core 3
            property.SetColumnType("decimal(18, 2)");
        }

        modelBuilder.Entity<CompetitorFleet>()
            .ToTable("CompetitorFleet")
            .HasKey(x => new { x.CompetitorId, x.FleetId });
        modelBuilder.Entity<SeriesRace>()
            .ToTable("SeriesRace")
            .HasKey(x => new { x.SeriesId, x.RaceId });
        modelBuilder.Entity<FleetBoatClass>()
            .ToTable("FleetBoatClass")
            .HasKey(x => new { x.FleetId, x.BoatClassId });
        modelBuilder.Entity<RegattaFleet>()
            .ToTable("RegattaFleet")
            .HasKey(x => new { x.RegattaId, x.FleetId });
        modelBuilder.Entity<RegattaSeries>()
            .ToTable("RegattaSeries")
            .HasKey(x => new { x.RegattaId, x.SeriesId });
        modelBuilder.Entity<SeriesToSeriesLink>()
            .ToTable("SeriesToSeriesLink");

        modelBuilder.Entity<Club>()
            .HasOne(c => c.DefaultScoringSystem)
            .WithMany(s => s.DefaultForClubs)
            .HasForeignKey(c => c.DefaultScoringSystemId);

        modelBuilder.Entity<Club>()
            .HasMany(c => c.ClubSequences)
            .WithOne(cs => cs.Club);

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

        modelBuilder.Entity<SeriesToSeriesLink>()
            .HasKey(sl => new { sl.ParentSeriesId, sl.ChildSeriesId });

        modelBuilder.Entity<SeriesToSeriesLink>()
            .ToTable("SeriesToSeriesLink");

        modelBuilder.Entity<SeriesToSeriesLink>()
            .HasOne(sl => sl.ParentSeries)
            .WithMany(s => s.ChildLinks)
            .HasForeignKey(sl => sl.ParentSeriesId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SeriesToSeriesLink>()
            .HasOne(sl => sl.ChildSeries)
            .WithMany(s => s.ParentLinks)
            .HasForeignKey(sl => sl.ChildSeriesId)
            .OnDelete(DeleteBehavior.Restrict);

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
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Regatta>()
            .HasMany(f => f.RegattaSeries)
            .WithOne(c => c.Regatta)
            .OnDelete(DeleteBehavior.Cascade);

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

        modelBuilder.Entity<SeriesForwarder>()
            .Property(b => b.Created)
            .HasDefaultValueSql("getdate()");
        modelBuilder.Entity<RegattaForwarder>()
            .Property(b => b.Created)
            .HasDefaultValueSql("getdate()");
        modelBuilder.Entity<CompetitorForwarder>()
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

        modelBuilder.Entity<Announcement>().HasQueryFilter(p => !p.IsDeleted);

        modelBuilder.Entity<CompetitorStatsSummary>(
            cs =>
            {
                cs.HasNoKey();
                cs.ToTable((string)null);
            });
        modelBuilder.Entity<CompetitorRankStats>(
            cs =>
            {
                cs.HasNoKey();
                cs.ToTable((string)null);
            });

        modelBuilder.Entity<ClubSeasonStats>(
            cs =>
            {
                cs.HasNoKey();
                cs.ToTable((string)null);
            });

        modelBuilder.Entity<SiteStats>(
            cs =>
            {
                cs.HasNoKey();
                cs.ToTable((string)null);
            });

        modelBuilder.Entity<AllCompHistogramStats>(
            cs =>
            {
                cs.HasNoKey();
                cs.ToTable((string)null);
            });

        modelBuilder.Entity<AllCompHistogramFields>(
            cs =>
            {
                cs.HasNoKey();
                cs.ToTable((string)null);
            });

        modelBuilder.Entity<DeletableInfo>(
            cs =>
            {
                cs.HasNoKey();
                cs.ToTable((string)null);
            });

        modelBuilder.Entity<CompetitorActiveDates>(
            cs =>
            {
                cs.HasNoKey();
                cs.ToTable((string)null);
            });

        modelBuilder.Entity<ChangeType>().HasData(
            new ChangeType { Id = ChangeType.CreatedId, Name = "Created" },
            new ChangeType { Id = ChangeType.DeletedId, Name = "Deleted" },
            new ChangeType { Id = ChangeType.ActivatedId, Name = "Activated" },
            new ChangeType { Id = ChangeType.DeactivatedId, Name = "Deactivated" },
            new ChangeType { Id = ChangeType.PropertyChangedId, Name = "Property Changed" },
            new ChangeType { Id = ChangeType.AdminNoteId, Name = "Admin Note" },
            new ChangeType { Id = ChangeType.MergedId, Name = "Merged" }
        );
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
#if DEBUG
        optionsBuilder.ConfigureWarnings(w =>
            w.Throw(RelationalEventId.MultipleCollectionIncludeWarning));

#endif
        base.OnConfiguring(optionsBuilder);
    }

}
