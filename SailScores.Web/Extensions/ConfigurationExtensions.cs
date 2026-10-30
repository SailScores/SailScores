using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace SailScores.Web.Extensions;

/// <summary>
/// Helpers for arranging configuration sources.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds an optional JSON file directly after appsettings.json, so that
    /// appsettings.{Environment}.json, user secrets, environment variables (App Service
    /// settings) and the command line all override it. Falls back to adding it last
    /// if appsettings.json is not a source.
    /// </summary>
    public static IConfigurationBuilder AddJsonFileAfterAppSettings(
        this IConfigurationBuilder builder,
        string path)
    {
        var source = new JsonConfigurationSource
        {
            Path = path,
            Optional = true,
            ReloadOnChange = true
        };

        var appSettingsIndex = builder.Sources
            .Select((s, i) => (Source: s, Index: i))
            .Where(x => x.Source is JsonConfigurationSource json
                && string.Equals(json.Path, "appsettings.json", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Index)
            .DefaultIfEmpty(-1)
            .First();

        if (appSettingsIndex < 0)
        {
            builder.Sources.Add(source);
        }
        else
        {
            builder.Sources.Insert(appSettingsIndex + 1, source);
        }

        return builder;
    }
}
