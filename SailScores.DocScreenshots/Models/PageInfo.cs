namespace SailScores.DocScreenshots.Models;

internal class PageInfo
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public Region? Region { get; set; }
}

internal class Region
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
}
