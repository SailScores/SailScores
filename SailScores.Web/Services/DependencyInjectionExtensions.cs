using Microsoft.Extensions.DependencyInjection;
using SailScores.Web.Services;

namespace SailScores.Web.Services
{
    public static class DependencyInjectionExtensions
    {
        public static void RegisterWebSailScoresServices(
            this IServiceCollection services)
        {
            services.AddScoped<ISeriesService, SeriesService>();
            services.AddScoped<IFleetService, FleetService>();
            services.AddScoped<IRaceService, RaceService>();
            services.AddScoped<IAuthorizationService, AuthorizationService>();

        }
    }
}
