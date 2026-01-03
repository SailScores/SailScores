namespace SailScores.DocScreenshots.Utils;

internal static class FileNameHelper
{
    public static string SanitizeFileName(string name)
    {
        foreach (var c in System.IO.Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    public static string BuildFileName(Models.PageInfo pageInfo, string mode)
    {
        var baseName = SanitizeFileName(pageInfo.Name);
        if (mode.ToLowerInvariant() == "light")
        {
            return baseName + ".png";
        }
        return baseName + $"-{mode}.png";
    }
}
