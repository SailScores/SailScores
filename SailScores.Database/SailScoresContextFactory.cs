using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace SailScores.Database;

// Allows `dotnet ef` to create the context without the web startup project.
// Used only at design time (migrations); never invoked at runtime.
public class SailScoresContextFactory : IDesignTimeDbContextFactory<SailScoresContext>
{
    public SailScoresContext CreateDbContext(string[] args)
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Development";

        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true);

        var configuration = configBuilder.Build();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found. " +
                "Please configure it in appsettings.Development.json or set the " +
                "ASPNETCORE_ENVIRONMENT variable.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<SailScoresContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new SailScoresContext(optionsBuilder.Options);
    }
}
