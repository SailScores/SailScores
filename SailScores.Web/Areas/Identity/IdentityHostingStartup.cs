using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(SailScores.Web.Areas.Identity.IdentityHostingStartup))]
namespace SailScores.Web.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) =>
            {
            });
        }
    }
}