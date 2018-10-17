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
                .ForMember(r => r.SeriesNames, o => o.MapFrom(s => s.Series.Select(sr => sr.Name)));

            CreateMap<Model.Club, Areas.Api.Models.ClubViewModel>();

        }
    }
}
