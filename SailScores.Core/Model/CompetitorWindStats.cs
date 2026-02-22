using System;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Core.Model;

/// <summary>
/// Represents a competitor's performance in a specific wind condition range
/// </summary>
public class CompetitorWindStats
{
    /// <summary>
    /// Wind speed range (e.g., "0-5 kts", "5-10 kts", "10-15 kts", "15-20 kts", "20+ kts")
    /// </summary>
    [Display(Name = "Wind Speed Range")]
    public string WindSpeedRange { get; set; }
    
    /// <summary>
    /// Midpoint of wind speed range in m/s for sorting and display
    /// </summary>
    public decimal WindSpeedMidpoint { get; set; }
    
    /// <summary>
    /// Wind direction range (e.g., "N", "NE", "E", etc.) - optional
    /// </summary>
    [Display(Name = "Wind Direction")]
    public string WindDirection { get; set; }
    
    /// <summary>
    /// Number of races in this wind condition
    /// </summary>
    [Display(Name = "Races")]
    public int RaceCount { get; set; }
    
    /// <summary>
    /// Average percent of starters beaten (0-100, where 100 is perfect and 0 is last)
    /// Higher is better. Calculated as (totalStarters - place) / (totalStarters - 1) * 100
    /// </summary>
    [Display(Name = "Avg % Beaten")]
    public decimal? AveragePercentPlace { get; set; }
    
    /// <summary>
    /// Average finishing position
    /// </summary>
    [Display(Name = "Avg Finish")]
    public decimal? AverageFinish { get; set; }
    
    /// <summary>
    /// Best finish in this wind condition
    /// </summary>
    [Display(Name = "Best Finish")]
    public int? BestFinish { get; set; }
    
    /// <summary>
    /// Number of races where competitor finished in top 3
    /// </summary>
    [Display(Name = "Podiums")]
    public int PodiumCount { get; set; }
    
    /// <summary>
    /// Number of wins in this wind condition
    /// </summary>
    [Display(Name = "Wins")]
    public int WinCount { get; set; }
}
