using Azure.Core;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SailScores.Web.Services;

public sealed class DataProtectionHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public DataProtectionHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var useBlobStorage = _configuration.GetValue("DataProtection:UseBlobStorage", false);
        var applicationName = _configuration["DataProtection:ApplicationName"] ?? "SailScores";
        var blobUri = _configuration["DataProtection:BlobUri"];

        try
        {
            if (useBlobStorage)
            {
                if (string.IsNullOrWhiteSpace(blobUri))
                {
                    return HealthCheckResult.Unhealthy(
                        $"Data protection health check failed. ApplicationName={applicationName}; BlobUri is not configured.");
                }

                var credential = CreateDefaultAzureCredential();
                var blobClient = new BlobClient(new Uri(blobUri, UriKind.Absolute), credential, null);
                var exists = await blobClient.ExistsAsync(cancellationToken);
                var blobExists = exists.Value;

                var healthDescription = blobExists
                    ? $"Data protection is using Azure Blob Storage. ApplicationName={applicationName}; BlobExists={blobExists}"
                    : $"Data protection is using Azure Blob Storage. ApplicationName={applicationName}; BlobExists={blobExists}; Blob may be created on first use";

                return HealthCheckResult.Healthy(healthDescription);
            }

            var defaultStoreDescription = $"Data protection is using the default key store. ApplicationName={applicationName}";
            return HealthCheckResult.Healthy(defaultStoreDescription);
        }
        catch (Exception ex)
        {
            var description = useBlobStorage
                ? $"Data protection health check failed. ApplicationName={applicationName}; Error={ex.Message}"
                : $"Data protection health check failed. ApplicationName={applicationName}; Error={ex.Message}";

            return HealthCheckResult.Unhealthy(description, ex);
        }
    }

    private static TokenCredential CreateDefaultAzureCredential()
    {
        var identityAssembly = Assembly.Load("Azure.Identity");
        var credentialType = identityAssembly.GetType("Azure.Identity.DefaultAzureCredential", throwOnError: true);
        var boolConstructor = credentialType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(constructor => constructor.GetParameters().Length == 1 && constructor.GetParameters()[0].ParameterType == typeof(bool));

        if (boolConstructor is not null)
        {
            return (TokenCredential)boolConstructor.Invoke(new object[] { false })!;
        }

        var optionsConstructor = credentialType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(constructor => constructor.GetParameters().Length == 1 && constructor.GetParameters()[0].ParameterType.FullName?.Contains("DefaultAzureCredentialOptions") == true);

        if (optionsConstructor is not null)
        {
            var optionsType = identityAssembly.GetType("Azure.Identity.DefaultAzureCredentialOptions", throwOnError: true);
            var options = Activator.CreateInstance(optionsType);
            return (TokenCredential)optionsConstructor.Invoke(new[] { options })!;
        }

        throw new MissingMethodException(credentialType.FullName, ".ctor");
    }

    private static string MaskBlobUri(string? blobUri)
    {
        if (string.IsNullOrWhiteSpace(blobUri))
        {
            return "(empty)";
        }

        try
        {
            return new Uri(blobUri, UriKind.Absolute).GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped);
        }
        catch
        {
            return blobUri;
        }
    }
}
