using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using SailScores.Web;


var builder = WebApplication.CreateBuilder(args);

// Load Application Insights configuration to enable adaptive sampling
builder.Host.ConfigureAppConfiguration((ctx, config) =>
{
    config.AddJsonFile("appsettings.ApplicationInsights.json", optional: true, reloadOnChange: true);
});

var startup = new Startup(builder.Configuration, builder.Environment);

startup.ConfigureServices(builder.Services);

var app = builder.Build();

startup.Configure(app, app.Environment);

app.Run();
