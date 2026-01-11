using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SailScores.Core.Model.BackupEntities;
using SailScores.Core.Services;
using CoreBackupService = SailScores.Core.Services.IBackupService;

namespace SailScores.Web.Services;

/// <summary>
/// Web layer service for club backup operations.
/// Handles compression/decompression and file operations.
/// </summary>
public class BackupService : Interfaces.IBackupService
{
    private readonly CoreBackupService _coreBackupService;
    private readonly Core.Services.IClubService _clubService;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        MaxDepth = 128
    };

    private static readonly JsonSerializerOptions _jsonReadOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public BackupService(
        CoreBackupService coreBackupService,
        Core.Services.IClubService clubService)
    {
        _coreBackupService = coreBackupService;
        _clubService = clubService;
    }

    public async Task<(byte[] Data, string FileName)> CreateBackupFileAsync(string clubInitials, string createdBy)
    {
        var clubId = await _clubService.GetClubId(clubInitials).ConfigureAwait(false);
        var backup = await _coreBackupService.CreateBackupAsync(clubId, createdBy).ConfigureAwait(false);

        // Serialize to JSON
        var json = JsonSerializer.Serialize(backup, _jsonOptions);
        var jsonBytes = Encoding.UTF8.GetBytes(json);

        // Compress with GZip
        using var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal, leaveOpen: true))
        {
            await gzipStream.WriteAsync(jsonBytes, 0, jsonBytes.Length).ConfigureAwait(false);
        }

        var compressedData = outputStream.ToArray();

        // Generate filename
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var fileName = $"{clubInitials}-backup-{timestamp}.json.gz";

        return (compressedData, fileName);
    }

    public async Task<(ClubBackupData Backup, BackupValidationResult Validation)> ReadBackupFileAsync(Stream stream)
    {
        try
        {
            // Read and decompress
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
            memoryStream.Position = 0;

            byte[] jsonBytes;

            // Check if it's GZip compressed (magic bytes: 1f 8b)
            var header = new byte[2];
            memoryStream.Read(header, 0, 2);
            memoryStream.Position = 0;

            if (header[0] == 0x1f && header[1] == 0x8b)
            {
                // GZip compressed
                using var decompressedStream = new MemoryStream();
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    await gzipStream.CopyToAsync(decompressedStream).ConfigureAwait(false);
                }
                jsonBytes = decompressedStream.ToArray();
            }
            else
            {
                // Assume uncompressed JSON
                jsonBytes = memoryStream.ToArray();
            }

            var json = Encoding.UTF8.GetString(jsonBytes);
            var backup = JsonSerializer.Deserialize<ClubBackupData>(json, _jsonReadOptions);

            var validation = _coreBackupService.ValidateBackup(backup);

            return (backup, validation);
        }
        catch (JsonException ex)
        {
            return (null, new BackupValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Invalid backup file format: {ex.Message}"
            });
        }
        catch (InvalidDataException)
        {
            return (null, new BackupValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid compressed file format."
            });
        }
        catch (Exception ex)
        {
            return (null, new BackupValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Error reading backup file: {ex.Message}"
            });
        }
    }

    public async Task<bool> RestoreBackupAsync(string clubInitials, ClubBackupData backup, bool preserveClubSettings = true)
    {
        var clubId = await _clubService.GetClubId(clubInitials).ConfigureAwait(false);
        return await _coreBackupService.RestoreBackupAsync(clubId, backup, preserveClubSettings).ConfigureAwait(false);
    }
}
