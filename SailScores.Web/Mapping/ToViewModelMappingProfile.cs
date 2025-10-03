using AutoMapper;
using SailScores.Api.Dtos;
using SailScores.Web.Models.SailScores;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Model = SailScores.Core.Model;
using Db = SailScores.Database.Entities;
using SailScores.Core.Model;
using SailScores.Core.Model.Summary;

namespace SailScores.Web.Mapping
{
    public class ToViewModelMappingProfile : Profile
    {
        public ToViewModelMappingProfile()
        {
            CreateMap<Model.Club, ClubSummaryViewModel>()
                .ForMember(d => d.CanEdit, o => o.Ignore())
                .ForMember(d => d.RecentRaces, o => o.Ignore())
                .ForMember(d => d.RecentSeries, o => o.Ignore())
                .ForMember(d => d.ImportantSeries, o => o.Ignore())
                .ForMember(d => d.UpcomingRaces, o => o.Ignore())
                .ForMember(d => d.CurrentRegattas, o => o.Ignore());

            CreateMap<ClubSummary, ClubSummaryViewModel>();

            CreateMap<Model.Club, AdminViewModel>()
                .ForMember(d => d.ScoringSystemOptions, o => o.Ignore())
                .ForMember(d => d.Latitude, o => o.MapFrom(s => s.WeatherSettings != null ? s.WeatherSettings.Latitude : null))
                .ForMember(d => d.Longitude, o => o.MapFrom(s => s.WeatherSettings != null ? s.WeatherSettings.Longitude : null))
                .ForMember(d => d.TemperatureUnits, o => o.MapFrom(s =>
                    s.WeatherSettings != null ? s.WeatherSettings.TemperatureUnits : null))
                .ForMember(d => d.SpeedUnits, o => o.MapFrom(s =>
                    s.WeatherSettings != null ? s.WeatherSettings.WindSpeedUnits : null))
                .ForMember(d => d.SpeedUnitOptions, o => o.Ignore())
                .ForMember(d => d.TemperatureUnitOptions, o => o.Ignore())
                .ForMember(d => d.LocaleOptions, o => o.Ignore())
                .ForMember(d => d.Locale, o => o.Ignore())
                .ForMember(d => d.Tips, o => o.Ignore())
                .ForMember(d => d.DefaultScoringSystemName, o => o.MapFrom(s => s.DefaultScoringSystem.Name))
                .ForMember(d => d.ShowClubInResults, o => o.MapFrom(s => s.ShowClubInResults ?? false))
                .ForMember(d => d.HasRaces, o => o.Ignore())
                .ForMember(d => d.HasCompetitors, o => o.Ignore())
                .ForMember(d => d.Users, o => o.Ignore())
                .ReverseMap()
                .ForMember(d => d.DefaultScoringSystem, o => o.Ignore());

            CreateMap<Model.Fleet, FleetSummary>()
                .ForMember(d => d.Series, o => o.Ignore());
            CreateMap<Model.Fleet, FleetWithOptionsViewModel>()
                .ForMember(d => d.BoatClassOptions, o => o.Ignore())
                .ForMember(d => d.BoatClassIds, o => o.MapFrom(s =>
                    s.BoatClasses.Select(c => c.Id)))
                .ForMember(d => d.CompetitorOptions, o => o.Ignore())
                .ForMember(d => d.CompetitorIds, o => o.MapFrom(s =>
                    s.Competitors.Select(c => c.Id)))
                .ForMember(d => d.CompetitorBoatClassOptions, o => o.Ignore())
                .ForMember(d => d.SuggestedFullName, o => o.Ignore())
                .ReverseMap();
            CreateMap<Model.Fleet, FleetDeleteViewModel>()
                .ForMember(d => d.IsDeletable, o => o.Ignore())
                .ForMember(d => d.PreventDeleteReason, o => o.Ignore());


            CreateMap<Model.Series, SeriesSummary>()
                .ForMember(d => d.FleetName, o => o.MapFrom(s =>
                    GetFleetsString(s.Races)));
            CreateMap<Model.Series, SeriesWithOptionsViewModel>()
                .ForMember(d => d.SeasonOptions, o => o.Ignore())
                .ForMember(d => d.SeasonId, o => o.MapFrom(s =>
                    s.Season.Id))
                .ForMember(d => d.ScoringSystemOptions, o => o.Ignore());

            CreateMap<Model.Competitor, CompetitorWithOptionsViewModel>()
                .ForMember(d => d.BoatClassOptions, o => o.Ignore())
                .ForMember(d => d.FleetOptions, o => o.Ignore())
                .ForMember(d => d.FleetIds, o => o.MapFrom(s => s.Fleets.Select(f => f.Id)));
            CreateMap<CompetitorViewModel, Model.Competitor>()
                .ForMember(d => d.ClubId, o => o.Ignore())
                .ForMember(d => d.AlternativeSailNumber, o => o.Ignore())
                .ForMember(d => d.IsActive, o => o.MapFrom(s => true))
                .ForMember(d => d.Notes, o => o.Ignore())
                .ForMember(d => d.BoatClassId, o => o.Ignore())
                .ForMember(d => d.BoatClass, o => o.Ignore())
                .ForMember(d => d.Fleets, o => o.Ignore())
                .ReverseMap();
            CreateMap<Model.Competitor, CompetitorStatsViewModel>()
                .ForMember(d => d.SeasonStats, o => o.Ignore());

            CreateMap<Model.Competitor, CompetitorIndexViewModel>()
                .ForMember(d => d.IsDeletable, o => o.Ignore())
                .ForMember(d => d.PreventDeleteReason, o => o.Ignore());

            CreateMap<Model.BoatClass, BoatClassDeleteViewModel>()
                .ForMember(d => d.IsDeletable, o => o.Ignore())
                .ForMember(d => d.PreventDeleteReason, o => o.Ignore());

            MapRaceObjects();

            CreateMap<Model.ScoringSystem, ScoringSystemWithOptionsViewModel>()
                .ForMember(d => d.ScoreCodeOptions, o => o.Ignore())
                .ForMember(d => d.ParentSystemOptions, o => o.Ignore());
            CreateMap<Model.ScoringSystem, ScoringSystemDeleteViewModel>()
                .ForMember(d => d.IsDeletable, o => o.Ignore())
                .ForMember(d => d.PreventDeleteReason, o => o.Ignore());
            CreateMap<Model.ScoreCode, ScoreCodeWithOptionsViewModel>()
                .ForMember(d => d.FormulaOptions, o => o.Ignore());

            CreateMap<Model.Score, ScoreViewModel>()
                .ForMember(d => d.ScoreCode, o => o.Ignore())
                .ForMember(d => d.CodePointsString, o => o.MapFrom(s
                    => s.CodePoints.HasValue ? s.CodePoints.Value.ToString("0.##", CultureInfo.CurrentCulture) : String.Empty))
                .ForMember(d => d.FinishTime, o => o.MapFrom(s => s.FinishTime))
                .ForMember(d => d.ElapsedTime, o => o.MapFrom(s => s.ElapsedTime))
                .ReverseMap()
                .ForMember(d => d.CodePoints, o => o.MapFrom(s => ParseDecimal(s.CodePointsString)))
                .ForMember(d => d.FinishTime, o => o.MapFrom(s => s.FinishTime))
                .ForMember(d => d.ElapsedTime, o => o.MapFrom(s => s.ElapsedTime));
            CreateMap<ScoreViewModel, ScoreDto>()
                .ForMember(d => d.CodePoints, o => o.MapFrom(s => ParseDecimal(s.CodePointsString)))
                .ReverseMap();
            CreateMap<ScoreCodeWithOptionsViewModel, ScoreCode>();

            CreateMap<Model.ClubRequest, ClubRequestViewModel>()
                .ReverseMap();
            CreateMap<Model.ClubRequest, ClubRequestWithOptionsViewModel>()
                .ForMember(d => d.ClubOptions, o => o.Ignore());


            CreateMap<Model.ClubRequest, AccountAndClubRequestViewModel>()
                .ForMember(d => d.ConfirmPassword, o => o.Ignore())
                .ForMember(d => d.ContactFirstName, o => o.Ignore())
                .ForMember(d => d.ContactLastName, o => o.Ignore())
                .ForMember(d => d.Password, o => o.Ignore())
                .ForMember(d => d.EnableAppInsights, o => o.Ignore())
                .ReverseMap();


            CreateMap<Model.Season, SeasonDeleteViewModel>()
                .ForMember(d => d.IsDeletable, o => o.Ignore())
                .ForMember(d => d.PreventDeleteReason, o => o.Ignore());


            CreateMap<Db.ClubSeasonStats, ClubSeasonStatsViewModel>();
            CreateMap<Db.SiteStats, AllClubStatsViewModel>();


            CreateMap<Model.Document, DocumentWithOptions>()
                .ForMember( d => d.TimeOffset, o => o.Ignore())
                .ForMember( d => d.File, o => o.Ignore());

            MapRegattaObjects();
        }

        private String GetFleetsString(IList<Race> races)
        {
            var fleetNames = races.Select(r => r.Fleet).Where(f => f != null)
                .Select(f => f.Name).Distinct();
            switch (fleetNames.Count())
            {
                case 0:
                    return "No Fleet";
                case 1:
                    return fleetNames.First();
                default:
                    return "Multiple Fleets";
            }
        }

        private void MapRaceObjects()
        {
            CreateMap<RaceViewModel, RaceWithOptionsViewModel>()
                .ForMember(d => d.FleetOptions, o => o.Ignore())
                .ForMember(d => d.SeriesOptions, o => o.Ignore())
                .ForMember(d => d.ScoreCodeOptions, o => o.Ignore())
                .ForMember(d => d.CompetitorOptions, o => o.Ignore())
                .ForMember(d => d.WeatherIconOptions, o => o.Ignore())
                .ForMember(d => d.InitialOrder, o => o.Ignore())
                .ForMember(d => d.Regatta, o => o.Ignore())
                .ForMember(d => d.RegattaId, o => o.MapFrom(s => s.Regatta != null ? s.Regatta.Id : (Guid?)null))
                .ForMember(d => d.Tips, o => o.Ignore())
                .ForMember(d => d.SeriesIds, o => o.MapFrom(s => s.Series.Select(sr => sr.Id)))
                .ForMember(d => d.CompetitorBoatClassOptions, o => o.Ignore())
                .ForMember(d => d.NeedsLocalDate, o => o.Ignore())
                .ForMember(d => d.ClubInitials, o => o.Ignore())
                .ForMember(d => d.UseAdvancedFeatures, o => o.Ignore())
                .ReverseMap();
            CreateMap<RaceWithOptionsViewModel, Model.Race>()
                .ForMember(d => d.Weather, o => o.Ignore())
                .ForMember(d => d.StartTime, o => o.MapFrom(s => s.StartTime))
                .ForMember(d => d.TrackTimes, o => o.MapFrom(s => s.TrackTimes));
            CreateMap<RaceViewModel, RaceDto>()
                .ForMember(d => d.ScoreIds, o => o.MapFrom(r => r.Scores.Select(s => s.Id)))
                .ForMember(d => d.RegattaId, o => o.MapFrom(r => r.Regatta.Id))
                .ForMember(d => d.SeriesIds, o => o.MapFrom(r => r.Series.Select(s => s.Id)))
                .ForMember(d => d.Weather, o => o.Ignore())
                .ReverseMap()
                .ForMember(d => d.Regatta, o => o.Ignore())
                .ForMember(d => d.Weather, o => o.Ignore());
            CreateMap<Model.Race, RaceViewModel>()
                .ForMember(r => r.Regatta, o => o.Ignore())
                .ForMember(r => r.Weather, o => o.Ignore())
                .ForMember(r => r.StartTime, o => o.MapFrom(s => s.StartTime))
                .ForMember(r => r.TrackTimes, o => o.MapFrom(s => s.TrackTimes));

            CreateMap<Model.Race, RaceSummaryViewModel>()
                .ForMember(r => r.FleetName, o => o.MapFrom(s => s.Fleet.Name))
                .ForMember(r => r.FleetShortName, o => o.MapFrom(s => s.Fleet.ShortName))
                .ForMember(r => r.SeriesUrlAndNames, o => o.MapFrom(s => s.Series.Select(sr => new KeyValuePair<string, string>(sr.UrlName, sr.Name))))
                .ForMember(r => r.Weather, o => o.Ignore());
        }

        private void MapRegattaObjects()
        {
            CreateMap<Model.Regatta, RegattaWithOptionsViewModel>()
                .ForMember(d => d.SeasonOptions, o => o.Ignore())
                .ForMember(d => d.ScoringSystemOptions, o => o.Ignore())
                .ForMember(d => d.FleetOptions, o => o.Ignore())
                .ForMember(d => d.FleetIds, o => o.MapFrom(s =>
                    s.Fleets.Select(c => c.Id)));

            CreateMap<Model.Regatta, RegattaViewModel>();
            CreateMap<Model.Regatta, RegattaSummaryViewModel>()
                .ForMember(d => d.ClubInitials, o => o.Ignore())
                .ForMember(d => d.ClubName, o => o.Ignore());
            CreateMap<RegattaViewModel, RegattaSummaryViewModel>()
                .ForMember(d => d.ClubInitials, o => o.Ignore())
                .ForMember(d => d.ClubName, o => o.Ignore())
                .ReverseMap()
                .ForMember(d => d.Fleets, o => o.Ignore());
        }

        private decimal? ParseDecimal(string decimalString)
        {
            decimal output;
            if (Decimal.TryParse(decimalString, out output))
            {
                return output;
            }
            if (Decimal.TryParse(decimalString, NumberStyles.Any, CultureInfo.InvariantCulture, out output))
            {
                return output;
            }
            return null;
        }
    }
}
