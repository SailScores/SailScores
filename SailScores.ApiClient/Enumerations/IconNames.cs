using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Enumerations
{
    public enum IconNames
    {
        [Display(Name = "Sunny")]
        Sunny = 1,
        [Display(Name = "Cloud")]
        Cloud = 2,
        [Display(Name = "Cloudy")]
        Cloudy = 3,
        [Display(Name = "Cloudy Gusts")]
        CloudyGusts = 4,
        [Display(Name = "Strong Wind")]
        StrongWind = 5,
        [Display(Name = "Sprinkle")]
        Sprinkle = 6,
        [Display(Name = "Showers")]
        Showers = 7,
        [Display(Name = "Rain")]
        Rain = 8,
        [Display(Name = "Rain Mix")]
        RainMix = 9,
        [Display(Name = "Snow")]
        Snow = 10,
        [Display(Name = "Hail")]
        Hail = 11,
        [Display(Name = "Storm Showers")]
        StormShowers = 12,
        [Display(Name = "Lightning")]
        Lightning = 13,
        [Display(Name = "Thunderstorm")]
        Thunderstorm = 14,
        [Display(Name = "Hot")]
        Hot = 15,
        [Display(Name = "Haze")]
        Haze = 16,
        [Display(Name = "Fog")]
        Fog = 17,
        [Display(Name = "Sml Craft Advis")]
        SmlCraftAdvis = 19,
        [Display(Name = "Gale Warning")]
        GaleWarning = 20,
        [Display(Name = "Storm Warning")]
        StormWarning = 21,
        [Display(Name = "Hurricane Warn")]
        HurricaneWarn = 22,
        [Display(Name = "Hurricane")]
        Hurricane = 23,
    }
}
