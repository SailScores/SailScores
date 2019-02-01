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
            CreateMap<Model.Race, RaceSummaryViewModel>()
                .ForMember(r => r.CompetitorCount, o => o.MapFrom(s => s.Scores.Count))
                .ForMember(r => r.FleetName, o => o.MapFrom(s => s.Fleet.Name))
                .ForMember(r => r.FleetShortName, o => o.MapFrom(s => s.Fleet.ShortName))
                .ForMember(r => r.SeriesNames, o => o.MapFrom(s => s.Series.Select(sr => sr.Name)));
            
            CreateMap<Model.Fleet, FleetSummary>()
                .ForMember(d => d.Series, o => o.Ignore());
            CreateMap<Model.Fleet, FleetWithOptionsViewModel>()
                .ForMember(d => d.BoatClassOptions, o => o.Ignore())
                .ForMember(d => d.BoatClassIds, o => o.MapFrom(s =>
                    s.BoatClasses.Select(c => c.Id)));

            CreateMap<Model.Series, SeriesWithOptionsViewModel>()
                .ForMember(d => d.SeasonOptions, o => o.Ignore())
                .ForMember(d => d.SeasonId, o => o.MapFrom(s =>
                    s.Season.Id));
            CreateMap<Model.Competitor, CompetitorWithOptionsViewModel>()
                .ForMember(d => d.BoatClassOptions, o => o.Ignore());
        }
    }
}
