namespace SailScores.Web.Models.SailScores;

public class SystemAlertViewModel
{
    public Guid Id { get; set; }
    public string HtmlContent { get; set; }
    public DateTime ExpiresUtc { get; set; }
}
