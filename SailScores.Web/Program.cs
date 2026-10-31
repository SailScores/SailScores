using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using SailScores.Web;
using SailScores.Web.Extensions;


var builder = WebApplication.CreateBuilder(args);

// Load OpenTelemetry configuration, overridable by environment-specific files and App Service settings
builder.Configuration.AddJsonFileAfterAppSettings("appsettings.OpenTelemetry.json");

var startup = new Startup(builder.Configuration, builder.Environment);

startup.ConfigureServices(builder.Services);

var app = builder.Build();

startup.Configure(app, app.Environment);

app.Run();
