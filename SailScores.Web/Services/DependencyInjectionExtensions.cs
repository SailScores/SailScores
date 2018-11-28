using Microsoft.Extensions.DependencyInjection;
using Sailscores.Web.Services;

namespace Sailscores.Web.Services
{
    public static class DependencyInjectionExtensions
    {
        public static void RegisterWebSailscoresServices(
            this IServiceCollection services)
        {
            services.AddScoped<ISeriesService, SeriesService>();
            services.AddScoped<IRaceService, RaceService>();

        }
    }
}
