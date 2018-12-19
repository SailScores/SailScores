using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Db = Sailscores.Database.Entities;
using Model = Sailscores.Core.Model;

namespace Sailscores.Core.Mapping
{
    public class DbToModelMappingProfile : Profile
    {
        public DbToModelMappingProfile()
        {
            // ToDo: Plenty more mappings to add, including many-to-many object collections.
            CreateMap<Db.Club, Model.Club>()
                .MaxDepth(2);
            CreateMap<Db.Competitor, Model.Competitor>()
                .MaxDepth(2);

            CreateMap<Db.Series, Model.Series>()
                .ForMember(d => d.Races, o => o.MapFrom(s => s.RaceSeries.Select(rs => rs.Race).ToList()))
                .ReverseMap()
                .ForMember(d => d.RaceSeries, o => o.Ignore());
            CreateMap<Db.Fleet, Model.Fleet>()
                .ForMember(d => d.Competitors, o => o.MapFrom(s => s.CompetitorFleets.Select(cf => cf.Competitor).ToList()));
            CreateMap<Db.Race, Model.Race>()
                .ReverseMap()
                .ForMember(d => d.SeriesRaces, o => o.Ignore());

        }
    }
}
