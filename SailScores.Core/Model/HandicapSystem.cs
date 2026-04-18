using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Core.Model;

public class HandicapSystem
{
    public Guid Id { get; set; }

    // null = site-wide standard; never modified by clubs
    public Guid? ClubId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    public HandicapSystemType SystemType { get; set; }

    [StringLength(2000)]
    public string Description { get; set; }
}

public enum HandicapSystemType
{
    // PHRF Time-on-Distance: corrected = elapsed_sec - (rating × distance_nm)
    PhrfToD = 1,
    // PHRF Time-on-Time: corrected = elapsed_sec × 600 / (600 + rating)
    PhrfToT = 2,
    // Portsmouth Yardstick: corrected = elapsed_sec / PY × 1000
    Portsmouth = 3,
    // Generic Time-on-Time with multiplier (covers IRC/ORC when TCC is stored as Value)
    TimeOnTime = 4,
    Custom = 99,
}
