using Ganss.Xss;
using Markdig;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services.Interfaces;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;

namespace SailScores.Web.Services;

public class SystemAlertService : ISystemAlertService
{
    private readonly Core.Services.ISystemAlertService _coreSystemAlertService;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly IMemoryCache _memoryCache;

    public SystemAlertService(
        Core.Services.ISystemAlertService coreSystemAlertService,
        IHtmlSanitizer htmlSanitizer,
        IMemoryCache memoryCache)
    {
        _coreSystemAlertService = coreSystemAlertService;
        _htmlSanitizer = htmlSanitizer;
        _memoryCache = memoryCache;
    }

    public async Task<IEnumerable<SystemAlertViewModel>> GetActiveAlertsAsync()
    {
        IEnumerable<Core.Model.SystemAlert> alerts = await GetActiveAlerts();

        var viewModels = new List<SystemAlertViewModel>();

        foreach (var alert in alerts)
        {
            var markdownHtml = Markdown.ToHtml(
                alert.Content ?? "",
                new MarkdownPipelineBuilder().UseAdvancedExtensions().Build());

            var sanitizedHtml = _htmlSanitizer.Sanitize(markdownHtml);

            sanitizedHtml = ProcessLocalTime(sanitizedHtml, alert.ExpiresUtc);

            viewModels.Add(new SystemAlertViewModel
            {
                Id = alert.Id,
                HtmlContent = sanitizedHtml,
                ExpiresUtc = alert.ExpiresUtc
            });
        }

        return viewModels;
    }

    private async Task<IEnumerable<Core.Model.SystemAlert>> GetActiveAlerts()
    {
        const string cacheKey = "SystemAlertService.ActiveAlerts";

        return await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            var alerts = await _coreSystemAlertService.GetActiveAlertsAsync();
            return alerts;
        });
    }

    private string ProcessLocalTime(string html, DateTime expiresUtc)
    {
        // Replace {{LOCALTIME}} with a span that will be updated with JavaScript
        var countdownPattern = @"\{\{LOCALTIME\}\}";
        var timeout = TimeSpan.FromSeconds(1);
        
        if (Regex.IsMatch(html, countdownPattern, RegexOptions.IgnoreCase, timeout))
        {
            var unixTimestamp = ((DateTimeOffset)expiresUtc).ToUnixTimeSeconds();
            var replacement = $"<span class=\"countdown-timer\" data-expires=\"{unixTimestamp}\"></span>";
            html = Regex.Replace(html, countdownPattern, replacement, RegexOptions.IgnoreCase, timeout);
        }
        
        return html;
    }
}
