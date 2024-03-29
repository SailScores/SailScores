﻿using SailScores.Api.Enumerations;

namespace SailScores.Database.Entities;

// Fleet is a group of competitors that may be scored against one another.
public class Fleet
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }

    [StringLength(30)]
    public String ShortName { get; set; }
    [StringLength(200)]
    public String Name { get; set; }

    [StringLength(30)]
    public String NickName { get; set; }

    [StringLength(2000)]
    public String Description { get; set; }

    // Should this fleet be shown on public club fleet lists.
    public bool IsHidden { get; set; }

    public bool? IsActive { get; set; }
    public FleetType FleetType { get; set; }
    public IList<FleetBoatClass> FleetBoatClasses { get; set; }
    public IList<CompetitorFleet> CompetitorFleets { get; set; }
}