using Ganss.Xss;
using Markdig;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services.Interfaces;
using System.Text.RegularExpressions;

namespace SailScores.Web.Services;

public class SystemAlertService : ISystemAlertService
{
    private readonly Core.Services.ISystemAlertService _coreSystemAlertService;
    private readonly IHtmlSanitizer _htmlSanitizer;

    public SystemAlertService(
        Core.Services.ISystemAlertService coreSystemAlertService,
        IHtmlSanitizer htmlSanitizer)
    {
        _coreSystemAlertService = coreSystemAlertService;
        _htmlSanitizer = htmlSanitizer;
    }

    public async Task<IEnumerable<SystemAlertViewModel>> GetActiveAlertsAsync()
    {
        var alerts = await _coreSystemAlertService.GetActiveAlertsAsync();
        
        var viewModels = new List<SystemAlertViewModel>();
        
        foreach (var alert in alerts)
        {
            var markdownHtml = Markdown.ToHtml(
                alert.Content ?? "", 
                new MarkdownPipelineBuilder().UseAdvancedExtensions().Build());
            
            var sanitizedHtml = _htmlSanitizer.Sanitize(markdownHtml);
            
            // Process time countdown if {{COUNTDOWN}} placeholder exists
            sanitizedHtml = ProcessCountdown(sanitizedHtml, alert.ExpiresUtc);
            
            viewModels.Add(new SystemAlertViewModel
            {
                Id = alert.Id,
                HtmlContent = sanitizedHtml,
                ExpiresUtc = alert.ExpiresUtc
            });
        }
        
        return viewModels;
    }
    
    private string ProcessCountdown(string html, DateTime expiresUtc)
    {
        // Replace {{COUNTDOWN}} with a span that will be updated with JavaScript
        var countdownPattern = @"\{\{COUNTDOWN\}\}";
        if (Regex.IsMatch(html, countdownPattern, RegexOptions.IgnoreCase))
        {
            var unixTimestamp = ((DateTimeOffset)expiresUtc).ToUnixTimeSeconds();
            var replacement = $"<span class=\"countdown-timer\" data-expires=\"{unixTimestamp}\"></span>";
            html = Regex.Replace(html, countdownPattern, replacement, RegexOptions.IgnoreCase);
        }
        
        return html;
    }
}
