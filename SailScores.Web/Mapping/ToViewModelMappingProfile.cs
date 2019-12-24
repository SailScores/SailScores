using AutoMapper;
using SailScores.Api.Dtos;
using SailScores.Web.Models.SailScores;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Db = SailScores.Database.Entities;
using Model = SailScores.Core.Model;

namespace SailScores.Web.Mapping
{
    public class ToViewModelMappingProfile : Profile
    {
        public ToViewModelMappingProfile()
        {
            CreateMap<Model.Club, ClubSummaryViewModel>()
                .ForMember(d => d.CanEdit, o => o.Ignore());

            CreateMap<Model.Club, AdminViewModel>()
                .ForMember(d => d.ScoringSystemOptions, o => o.Ignore())
                .ForMember(d => d.Tips, o => o.Ignore())
                .ForMember(d => d.DefaultScoringSystemName, o => o.MapFrom(s => s.DefaultScoringSystem.Name))
                .ReverseMap()
                .ForMember(d => d.DefaultScoringSystem, o => o.Ignore());
            CreateMap<Model.Race, RaceSummaryViewModel>()
                .ForMember(r => r.FleetName, o => o.MapFrom(s => s.Fleet.Name))
                .ForMember(r => r.FleetShortName, o => o.MapFrom(s => s.Fleet.ShortName))
                .ForMember(r => r.SeriesNames, o => o.MapFrom(s => s.Series.Select(sr => sr.Name)));
            CreateMap<Model.Score, ScoreViewModel>()
                .ForMember(d => d.ScoreCode, o => o.Ignore())
                .ForMember(d => d.CodePointsString, o => o.MapFrom(s => s.CodePoints.HasValue ? s.CodePoints.Value.ToString("0.##") : String.Empty))
                .ReverseMap()
                .ForMember(d => d.CodePoints, o => o.MapFrom(s => ParseDecimal(s.CodePointsString)));
            CreateMap<Model.Fleet, FleetSummary>()
                .ForMember(d => d.Series, o => o.Ignore());
            CreateMap<Model.Fleet, FleetWithOptionsViewModel>()
                .ForMember(d => d.BoatClassOptions, o => o.Ignore())
                .ForMember(d => d.BoatClassIds, o => o.MapFrom(s =>
                    s.BoatClasses.Select(c => c.Id)))
                .ForMember(d => d.CompetitorOptions, o => o.Ignore())
                .ForMember(d => d.CompetitorIds, o => o.MapFrom(s =>
                    s.Competitors.Select(c => c.Id)))
                .ForMember(d => d.Regatta, o => o.Ignore())
                .ForMember(d => d.RegattaId, o => o.Ignore());

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
                .ForMember(d => d.Fleets, o => o.Ignore());


            CreateMap<RaceViewModel, RaceWithOptionsViewModel>()
                .ForMember(d => d.FleetOptions, o => o.Ignore())
                .ForMember(d => d.SeriesOptions, o => o.Ignore())
                .ForMember(d => d.ScoreCodeOptions, o => o.Ignore())
                .ForMember(d => d.CompetitorOptions, o => o.Ignore())
                .ForMember(d => d.InitialOrder, o => o.Ignore())
                .ForMember(d => d.Regatta, o => o.Ignore())
                .ForMember(d => d.RegattaId, o => o.Ignore())
                .ForMember(d => d.Tips, o => o.Ignore())
                .ForMember(d => d.SeriesIds, o => o.MapFrom(s => s.Series.Select(sr => sr.Id)))
                .ForMember(d => d.CompetitorBoatClassOptions, o => o.Ignore());

            CreateMap<Model.ScoringSystem, ScoringSystemWithOptionsViewModel>()
                .ForMember(d => d.ScoreCodeOptions, o => o.Ignore())
                .ForMember(d => d.ParentSystemOptions, o => o.Ignore());

            CreateMap<Model.ScoringSystem, ScoringSystemCanBeDeletedViewModel>()
                .ForMember(d => d.InUse, o => o.Ignore());

            CreateMap<Model.ScoreCode, ScoreCodeWithOptionsViewModel>()
                .ForMember(d => d.FormulaOptions, o => o.Ignore());

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

            CreateMap<RaceViewModel, RaceDto>()
                .ForMember(d => d.ScoreIds, o => o.MapFrom(r => r.Scores.Select(s => s.Id)))
                .ForMember(d => d.RegattaId, o => o.MapFrom(r => r.Regatta.Id))
                .ForMember(d => d.SeriesIds, o => o.MapFrom(r => r.Series.Select(s => s.Id)))
                .ReverseMap()
                .ForMember(d => d.Regatta, o => o.Ignore());
            CreateMap<Model.Race, RaceViewModel>()
                .ForMember(r => r.Regatta, o => o.Ignore());
            CreateMap<ScoreViewModel, ScoreDto>()
                .ForMember(d => d.CodePoints, o => o.MapFrom(s => ParseDecimal(s.CodePointsString)))
                .ReverseMap();

            CreateMap<Model.Series, SeriesSummary>();
            CreateMap<Model.ClubRequest, ClubRequestViewModel>()
                .ReverseMap();
            CreateMap<Model.ClubRequest, ClubRequestWithOptionsViewModel>()
                .ForMember(d => d.ClubOptions, o => o.Ignore());
        }

        private decimal? ParseDecimal(string decimalString)
        {
            decimal output;
            if(Decimal.TryParse(decimalString, out output))
            {
                return output;
            }
            if(Decimal.TryParse(decimalString, NumberStyles.Any, CultureInfo.InvariantCulture, out output))
            {
                return output;
            }
            return null;
        }
    }
}
