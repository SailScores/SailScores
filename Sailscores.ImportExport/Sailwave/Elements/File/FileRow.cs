namespace Sailscores.ImportExport.Sailwave.Elements.File
{
    public class FileRow
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public int? CompetitorOrScoringSystemId { get; set; }
        public int? RaceId { get; set; }
        public RowType RowType { get; set; }
    }
}
