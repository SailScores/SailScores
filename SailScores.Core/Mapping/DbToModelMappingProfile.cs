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
            CreateMap<Db.Competitor, Model.Competitor>()
                .ForMember(d => d.Fleets, o => o.MapFrom(s => s.CompetitorFleets.Select(f => f.Fleet).ToList()));

            CreateMap<Db.Series, Model.Series>()
                .ForMember(d => d.Races, o => o.MapFrom(s => s.RaceSeries.Select(rs => rs.Race).ToList()))
                .ForMember(d => d.Results, o => o.Ignore())
                .ForMember(d => d.FlatResults, o => o.Ignore())
                .ForMember(d => d.Competitors, o => o.MapFrom(s =>
                        s.RaceSeries
                        .SelectMany(rs => rs.Race.Scores
                            .Select(r => r.Competitor)).Distinct().ToList()))
                .ReverseMap()
                .ForMember(d => d.RaceSeries, o => o.Ignore());
            CreateMap<Db.Fleet, Model.Fleet>()
                .ForMember(d => d.Competitors, o => o.MapFrom(s => s.CompetitorFleets.Select(cf => cf.Competitor).ToList()))
                .ForMember(d => d.BoatClasses, o => o.MapFrom(s => s.FleetBoatClasses.Select(fbc => fbc.BoatClass).ToList()));
            CreateMap<Db.Race, Model.Race>()
                .ForMember(d => d.Series, o => o.MapFrom(s => s.SeriesRaces.Select(f => f.Series).ToList()))
                .ForMember(d => d.Season, o => o.Ignore())
                .ReverseMap()
                .ForMember(d => d.SeriesRaces, o => o.Ignore());
            CreateMap<Db.Score, Model.Score>()
                .ForMember(d => d.Competitor, o => o.Ignore());

        }
    }
}
