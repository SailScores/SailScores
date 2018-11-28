using Microsoft.Extensions.DependencyInjection;
using Sailscores.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sailscores.Core.Services
{
    public static class DependencyInjectionExtensions
    {
        public static void RegisterCoreSailscoresServices(
            this IServiceCollection services)
        {
                services.AddScoped<IClubService, ClubService>();
                services.AddScoped<ICompetitorService, CompetitorService>();
                services.AddScoped<IScoringService, ScoringService>();
                services.AddScoped<ISeriesService, SeriesService>();
                services.AddScoped<IRaceService, RaceService>();
                services.AddScoped<ISeriesService, SeriesService>();
                services.AddScoped<Sailscores.Core.Scoring.ISeriesCalculator, Sailscores.Core.Scoring.SeriesCalculator>();

        }
    }
}
