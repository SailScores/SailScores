namespace SailScores.Core.Services;

public static class ImageContentTypeDetector
{
    public static string Detect(byte[] fileContents)
    {
        if (fileContents == null || fileContents.Length < 4)
        {
            return "image/png"; // default
        }

        // Check file signatures (magic numbers) for supported formats only
        if (fileContents[0] == 0x89 && fileContents[1] == 0x50 && fileContents[2] == 0x4E && fileContents[3] == 0x47)
            return "image/png";
        if (fileContents[0] == 0xFF && fileContents[1] == 0xD8 && fileContents[2] == 0xFF)
            return "image/jpeg";
        if (fileContents[0] == 0x47 && fileContents[1] == 0x49 && fileContents[2] == 0x46)
            return "image/gif";
        // WebP: RIFF....WEBP (check for "RIFF" at start and "WEBP" at bytes 8-11)
        if (fileContents.Length >= 12 &&
            fileContents[0] == 0x52 && fileContents[1] == 0x49 && fileContents[2] == 0x46 && fileContents[3] == 0x46 &&
            fileContents[8] == 0x57 && fileContents[9] == 0x45 && fileContents[10] == 0x42 && fileContents[11] == 0x50)
            return "image/webp";

        return "image/png"; // default fallback
    }
}
