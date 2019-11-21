using Microsoft.Extensions.DependencyInjection;
using SailScores.Core.Scoring;
using SailScores.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SailScores.Core.Services
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
            services.AddScoped<IRegattaService, RegattaService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IScoringCalculatorFactory, ScoringCalculatorFactory>();
            services.AddScoped<IMergeService, MergeService>();
        }
    }
}
