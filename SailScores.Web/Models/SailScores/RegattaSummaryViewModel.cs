﻿using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

public class RegattaSummaryViewModel
{

    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public String Name { get; set; }
    public String UrlName { get; set; }
    public String Description { get; set; }
    public IList<FleetSummary> Fleets { get; set; }
    public Season Season { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public String ClubInitials { get; set; }
    public String ClubName { get; set; }

    public String FleetCountString
    {
        get
        {
            var count = this.Fleets?.Count ?? 0;
            switch (count)
            {
                case 0:
                    return "No fleets";
                case 1:
                    return "1 fleet";
                default:
                    return $"{count} fleets";
            }
        }
    }
}