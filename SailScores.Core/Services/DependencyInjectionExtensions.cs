using Microsoft.Extensions.DependencyInjection;
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
                services.AddScoped<IClubService, ClubService>();
                services.AddScoped<ICompetitorService, CompetitorService>();
                services.AddScoped<IScoringService, ScoringService>();
                services.AddScoped<ISeriesService, SeriesService>();
                services.AddScoped<IRaceService, RaceService>();
                services.AddScoped<ISeriesService, SeriesService>();
                services.AddScoped<SailScores.Core.Scoring.ISeriesCalculator, SailScores.Core.Scoring.SeriesCalculator>();

        }
    }
}
