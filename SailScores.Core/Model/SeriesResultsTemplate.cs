using SailScores.Api.Enumerations;
using System;

namespace SailScores.Core.Model;

public class SeriesResultsTemplate
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public string Name { get; set; }

    // Column visibility settings
    public ColumnVisibility SailNumberVisibility { get; set; }
    public ColumnVisibility CompetitorNameVisibility { get; set; }
    public string CompetitorNameHeader { get; set; }
    public ColumnVisibility BoatNameVisibility { get; set; }
    public string BoatNameHeader { get; set; }
    public ColumnVisibility CompetitorClubVisibility { get; set; }
}
