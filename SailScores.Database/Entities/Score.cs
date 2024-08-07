﻿namespace SailScores.Database.Entities;

public class Score
{
    public Guid Id { get; set; }
    public Competitor Competitor { get; set; }
    public Guid CompetitorId { get; set; }
    public Race Race { get; set; }
    public Guid RaceId { get; set; }
    public int? Place { get; set; }

    [StringLength(20)]
    public string Code { get; set; }
    public decimal? CodePoints { get; set; }
}