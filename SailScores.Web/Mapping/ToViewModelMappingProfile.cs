using AutoMapper;
using Sailscores.Web.Models.Sailscores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Db = Sailscores.Database.Entities;
using Model = Sailscores.Core.Model;

namespace Sailscores.Web.Mapping
{
    public class ToViewModelMappingProfile : Profile
    {
        public ToViewModelMappingProfile()
        {
            CreateMap<Model.Race, RaceSummaryViewModel>()
                .ForMember(r => r.CompetitorCount, o => o.MapFrom(s => s.Scores.Count))
                .ForMember(r => r.FleetName, o => o.MapFrom(s => s.Fleet.Name))
                .ForMember(r => r.SeriesNames, o => o.MapFrom(s => s.Series.Select(sr => sr.Name)));
            
            CreateMap<Model.Fleet, FleetSummary>()
                .ForMember(d => d.Series, o => o.Ignore());

        }
    }
}
