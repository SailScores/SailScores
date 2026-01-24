using Microsoft.AspNetCore.Http;
using SailScores.Core.Model;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.SailScores;

// View model for editing club information with file upload support
public class AdminEditViewModel
{

#pragma warning disable CA2227 // Collection properties should be read only
    public Guid Id { get; set; }

    [Required]
    [StringLength(200)]
    public String Name { get; set; }

    [StringLength(10)]
    public String Initials { get; set; }
    
    [Display(Name = "Public Description")]
    public String Description { get; set; }
    
    [Display(Name = "Home Page Description")]
    public String HomePageDescription { get; set; }
    
    public Guid? LogoFileId { get; set; }
    
    [Display(Name = "Burgee/Logo File")]
    public IFormFile LogoFile { get; set; }
    
    public bool IsHidden { get; set; }
    public bool ShowClubInResults { get; set; }
    
    [Display(Name = "Show Calendar in Navigation")]
    public bool ShowCalendarInNav { get; set; }

    public String Url { get; set; }

    public IList<SeasonDeleteViewModel> Seasons { get; set; }

    public string DefaultScoringSystemName { get; set; }
    public Guid? DefaultScoringSystemId { get; set; }

    public IList<ScoringSystem> ScoringSystemOptions { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string TemperatureUnits { get; set; }
    public string SpeedUnits { get; set; }
    public IList<string> SpeedUnitOptions { get; set; }
    public IList<string> TemperatureUnitOptions { get; set; }

    public int? DefaultRaceDateOffset { get; set; }

    public string Locale { get; set; }
    public IList<string> LocaleOptions { get; set; }

    public int RaceCount { get; set; }
    
    public bool CanSelfReset => RaceCount <= ResetClubViewModel.MaxSelfServiceRaceCount;

#pragma warning restore CA2227 // Collection properties should be read only
}
