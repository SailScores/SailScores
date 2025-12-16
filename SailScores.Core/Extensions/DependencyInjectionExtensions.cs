using Microsoft.Extensions.DependencyInjection;
using SailScores.Core.Scoring;
using SailScores.Core.Services;

namespace SailScores.Core.Extensions
{
    public static class DependencyInjectionExtensions
    {
        public static void RegisterCoreSailScoresServices(
            this IServiceCollection services)
        {
            services.AddScoped<IDbObjectBuilder, DbObjectBuilder>();
            services.AddScoped<IClubService, ClubService>();
            services.AddScoped<IBoatClassService, BoatClassService>();
            services.AddScoped<IFleetService, FleetService>();
            services.AddScoped<ISeasonService, SeasonService>();
            services.AddScoped<ICompetitorService, CompetitorService>();
            services.AddScoped<IScoringService, ScoringService>();
            services.AddScoped<ISeriesService, SeriesService>();
            services.AddScoped<IRaceService, RaceService>();
            services.AddScoped<ISeriesService, SeriesService>();
            services.AddScoped<IAnnouncementService, AnnouncementService>();
            services.AddScoped<IRegattaService, RegattaService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IScoringCalculatorFactory, ScoringCalculatorFactory>();
            services.AddScoped<IClubRequestService, ClubRequestService>();
            services.AddScoped<IMergeService, MergeService>();
            services.AddScoped<IWeatherService, WeatherService>();
            services.AddScoped<IConversionService, ConversionService>();
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddScoped<IForwarderService, ForwarderService>();
            services.AddScoped<ISystemAlertService, SystemAlertService>();
            services.AddScoped<IMatchingService, MatchingService>();
            services.AddScoped<ISupporterService, SupporterService>();
            services.AddScoped<ICoreCalendarService, CalendarService>();
            services.AddScoped<IReportService, ReportService>();

        }
    }
}
