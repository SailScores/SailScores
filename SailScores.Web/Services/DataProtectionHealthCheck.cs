using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;

namespace SailScores.Web.Services;

public sealed class DataProtectionHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly IXmlRepository _xmlRepository;

    public DataProtectionHealthCheck(IConfiguration configuration, IXmlRepository xmlRepository)
    {
        _configuration = configuration;
        _xmlRepository = xmlRepository;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var useBlobStorage = _configuration.GetValue("DataProtection:UseBlobStorage", false);
        var applicationName = _configuration["DataProtection:ApplicationName"] ?? "SailScores";
        var blobUri = _configuration["DataProtection:BlobUri"];

        try
        {
            var keyIds = _xmlRepository.GetAllElements()
                .Select(element => element.Attribute("id")?.Value ?? "unknown")
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();

            var description = useBlobStorage
                ? $"Data protection is using Azure Blob Storage. ApplicationName={applicationName}; BlobUri={blobUri}; KeyCount={keyIds.Count}"
                : $"Data protection is using the default key store. ApplicationName={applicationName}; KeyCount={keyIds.Count}";

            return Task.FromResult(HealthCheckResult.Healthy(description));
        }
        catch (Exception ex)
        {
            var description = useBlobStorage
                ? $"Data protection health check failed. ApplicationName={applicationName}; BlobUri={blobUri}; Error={ex.Message}"
                : $"Data protection health check failed. ApplicationName={applicationName}; Error={ex.Message}";

            return Task.FromResult(HealthCheckResult.Unhealthy(description, ex));
        }
    }
}
