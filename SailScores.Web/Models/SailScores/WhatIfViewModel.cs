using SailScores.Core.Model;
using System.ComponentModel;

namespace SailScores.Web.Models.SailScores;

public class WhatIfViewModel
{
    public IList<ScoringSystem> ScoringSystemOptions { get; set; }
    public Core.Model.Series Series { get; set; }

    public Guid? SeriesId { get; set; }
    public Guid? SelectedScoringSystemId { get; set; }
    public int Discards { get; set; }

    [DisplayName("Participation Percent")]
    public Decimal? ParticipationPercent { get; set; }

}