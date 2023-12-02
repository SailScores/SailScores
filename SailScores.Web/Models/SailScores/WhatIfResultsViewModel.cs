using SailScores.Core.Model;
using System.ComponentModel;

namespace SailScores.Web.Models.SailScores;

public class WhatIfResultsViewModel
{
    public Guid SeriesId { get; set; }
    public Core.Model.Series Series { get; set; }

    public Core.FlatModel.FlatResults AlternateResults { get; set; }
}