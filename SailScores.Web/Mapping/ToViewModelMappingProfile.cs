using AutoMapper;
using SailScores.Web.Models.SailScores;
using System;
using System.Collections.Generic;
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
                .ForMember(d => d.ScoringSystemOptions, o => o.Ignore());
            CreateMap<Model.Race, RaceSummaryViewModel>()
                .ForMember(r => r.FleetName, o => o.MapFrom(s => s.Fleet.Name))
                .ForMember(r => r.FleetShortName, o => o.MapFrom(s => s.Fleet.ShortName))
                .ForMember(r => r.SeriesNames, o => o.MapFrom(s => s.Series.Select(sr => sr.Name)));
            CreateMap<Model.Score, ScoreViewModel>()
                .ForMember(s => s.ScoreCode, o => o.Ignore());

            CreateMap<Model.Fleet, FleetSummary>()
                .ForMember(d => d.Series, o => o.Ignore());
            CreateMap<Model.Fleet, FleetWithOptionsViewModel>()
                .ForMember(d => d.BoatClassOptions, o => o.Ignore())
                .ForMember(d => d.BoatClassIds, o => o.MapFrom(s =>
                    s.BoatClasses.Select(c => c.Id)))
                .ForMember(d => d.CompetitorOptions, o => o.Ignore())
                .ForMember(d => d.CompetitorIds, o => o.MapFrom(s =>
                    s.Competitors.Select(c => c.Id))); ;

            CreateMap<Model.Series, SeriesWithOptionsViewModel>()
                .ForMember(d => d.SeasonOptions, o => o.Ignore())
                .ForMember(d => d.SeasonId, o => o.MapFrom(s =>
                    s.Season.Id));
            CreateMap<Model.Competitor, CompetitorWithOptionsViewModel>()
                .ForMember(d => d.BoatClassOptions, o => o.Ignore());

            CreateMap<RaceViewModel, RaceWithOptionsViewModel>()
                .ForMember(d => d.FleetOptions, o => o.Ignore())
                .ForMember(d => d.SeriesOptions, o => o.Ignore())
                .ForMember(d => d.ScoreCodeOptions, o => o.Ignore())
                .ForMember(d => d.CompetitorOptions, o => o.Ignore())
                .ForMember(d => d.SeriesIds, o => o.MapFrom(s => s.Series.Select(sr => sr.Id)));

            CreateMap<Model.ScoringSystem, ScoringSystemWithOptionsViewModel>()
                .ForMember(d => d.ScoreCodeOptions, o => o.Ignore())
                .ForMember(d => d.ParentSystemOptions, o => o.Ignore());

            CreateMap<Model.ScoreCode, ScoreCodeWithOptionsViewModel>()
                .ForMember(d => d.FormulaOptions, o => o.Ignore());
        }
    }
}
