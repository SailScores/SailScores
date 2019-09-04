using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dto = SailScores.Api.Dtos;
using Model = SailScores.Core.Model;

namespace SailScores.Core.Mapping
{
    public class ModelToDtoMappingProfile : Profile
    {
        public ModelToDtoMappingProfile()
        {
            CreateMap<Model.BoatClass, Dto.BoatClassDto>()
                .ReverseMap()
                .ForMember(d => d.Club, o => o.Ignore());
            CreateMap<Model.Club, Dto.ClubDto>()
                .ForMember(d => d.CompetitorIds, o => o.MapFrom(s => s.Competitors.Select(c => c.Id)))
                .ForMember(d => d.FleetIds, o => o.MapFrom(s => s.Fleets.Select(c => c.Id)))
                .ForMember(d => d.BoatClassIds, o => o.MapFrom(s => s.BoatClasses.Select(c => c.Id)))
                .ForMember(d => d.SeasonIds, o => o.MapFrom(s => s.Seasons.Select(c => c.Id)))
                .ForMember(d => d.RaceIds, o => o.MapFrom(s => s.Races.Select(c => c.Id)))
                .ForMember(d => d.ScoringSystemIds, o => o.MapFrom(s => s.ScoringSystems.Select(c => c.Id)))
                .ForMember(d => d.DefaultScoringSystemId, o => o.MapFrom(s => s.DefaultScoringSystem.Id))
                .ForMember(d => d.SeriesIds, o => o.MapFrom(s => s.Series.Select(c => c.Id)))
                .ReverseMap();
            CreateMap<Model.Competitor, Dto.CompetitorDto>()
                .ForMember(d => d.FleetIds, o => o.MapFrom(s => s.Fleets.Select(c => c.Id)))
                .ForMember(d => d.ScoreIds, o => o.MapFrom(s => s.Scores.Select(c => c.Id)))
                .ReverseMap()
                .ForMember(d => d.BoatClass, o => o.Ignore())
                .ForMember(d => d.Scores, o => o.Ignore());

            CreateMap<Model.Fleet, Dto.FleetDto>()
                .ForMember(d => d.BoatClassIds, o => o.MapFrom(s => s.BoatClasses.Select(c => c.Id)))
                .ForMember(d => d.CompetitorIds, o => o.MapFrom(s => s.Competitors.Select(c => c.Id)))
                .ReverseMap();
            CreateMap<Model.Race, Dto.RaceDto>()
                .ForMember(d => d.ScoreIds, o => o.MapFrom(s => s.Scores.Select(c => c.Id)))
                .ForMember(d => d.SeriesIds, o => o.MapFrom(s => s.Series.Select(c => c.Id)))
                .ForMember(d => d.RegattaId, o => o.Ignore());
            CreateMap<Model.ScoreCode, Dto.ScoreCodeDto>();
            CreateMap<Model.Score, Dto.ScoreDto>();
            CreateMap<Model.Season, Dto.SeasonDto>()
                .ForMember(d => d.SeriesIds, o => o.MapFrom(s => s.Series.Select(c => c.Id)))
                .ReverseMap()
                .ForMember(d => d.Series, o => o.Ignore());
            CreateMap<Model.Series, Dto.SeriesDto>()
                .ForMember(d => d.RaceIds, o => o.MapFrom(s => s.Races.Select(c => c.Id)))
                .ReverseMap();

        }
    }
}
