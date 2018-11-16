using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Db = SailScores.Database.Entities;
using Model = SailScores.Core.Model;

namespace SailScores.Core.Mapping
{
    public class DbToModelMappingProfile : Profile
    {
        public DbToModelMappingProfile()
        {
            // ToDo: Plenty more mappings to add, including many-to-many object collections.
            CreateMap<Db.Club, Model.Club>();
            CreateMap<Db.Competitor, Model.Competitor>();

            CreateMap<Db.Series, Model.Series>()
                .ForMember(d => d.Races, o => o.MapFrom(s => s.RaceSeries.Select(rs => rs.Race).ToList()));
            CreateMap<Db.Fleet, Model.Fleet>()
                .ForMember(d => d.Competitors, o => o.MapFrom(s => s.CompetitorFleets.Select(cf => cf.Competitor).ToList()));
            CreateMap<Db.Race, Model.Race>();

        }
    }
}
