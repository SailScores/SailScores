using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SailScores.Core.Utility
{
    public static class UrlUtility
    {
        public static string GetUrlName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                return null;
            }

            // Normalize the string to decompose characters
            string normalizedString = rawName.Normalize(NormalizationForm.FormD);

            // Remove diacritics
            StringBuilder stringBuilder = new StringBuilder();
            foreach (char c in normalizedString)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            // Convert to lowercase
            string result = stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();

            // Replace spaces with hyphens
            result = Regex.Replace(result, @"\s+", "-");

            // Remove invalid URL characters
            result = Regex.Replace(result, @"[^a-z0-9\-]", "");

            // Remove multiple hyphens
            result = Regex.Replace(result, @"-+", "-").Trim('-');

            return result;
        }

        public static string EnsureHttpPrefix(string url)
        {
            if (url == null)
            {
                return null;
            }
            if (string.IsNullOrWhiteSpace(url))
            {
                return String.Empty;
            }

            string trimmed = url.Trim();

            if (trimmed.StartsWith("//", StringComparison.Ordinal))
            {
                return "https:" + trimmed;
            }

            Uri uri;
            if (Uri.TryCreate(trimmed, UriKind.Absolute, out uri))
            {
                var builder = new UriBuilder(uri);
                builder.Scheme = Uri.UriSchemeHttps;
                return builder.ToString();
            }
            else
            {
                // Assume it's a host or relative path, prepend https://
                return "https://" + trimmed;
            }
        }
    }
}
