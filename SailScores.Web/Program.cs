using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using SailScores.Web;


var builder = WebApplication.CreateBuilder(args);

// Load OpenTelemetry configuration
builder.Host.ConfigureAppConfiguration((ctx, config) =>
{
    config.AddJsonFile("appsettings.OpenTelemetry.json", optional: true);
});

var startup = new Startup(builder.Configuration, builder.Environment);

startup.ConfigureServices(builder.Services);

var app = builder.Build();

startup.Configure(app, app.Environment);

app.Run();
