using Microsoft.AspNetCore.Http;

namespace SailScores.Web.Models.SailScores;

public class DocumentWithOptions : Core.Model.Document
{
    public IFormFile File { get; set; }
    public int TimeOffset { get; set; }
}