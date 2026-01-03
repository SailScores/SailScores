namespace SailScores.Web.Models.SailScores;

public class IcalImportResult
{
    public List<ImportedSeries> Series { get; set; } = new List<ImportedSeries>();
    public string Warning { get; set; }
}

public class ImportedSeries
{
    public string Name { get; set; }
    public string StartDate { get; set; }
    public string EndDate { get; set; }
}
