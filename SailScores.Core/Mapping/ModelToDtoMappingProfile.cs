using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dto = Sailscores.Core.Model.Dto;
using Model = Sailscores.Core.Model;

namespace Sailscores.Core.Mapping
{
    public class ModelToDtoMappingProfile : Profile
    {
        public ModelToDtoMappingProfile()
        {
            CreateMap<Model.BoatClass, Dto.BoatClassDto>();
            CreateMap<Model.Club, Dto.ClubDto>()
                .ForMember(d => d.CompetitorIds, o => o.MapFrom(s => s.Competitors.Select(c => c.Id)));
            CreateMap<Model.Competitor, Dto.CompetitorDto>();
            CreateMap<Model.Fleet, Dto.FleetDto>();
            CreateMap<Model.Race, Dto.RaceDto>();
            CreateMap<Model.ScoreCode, Dto.ScoreCodeDto>();
            CreateMap<Model.Score, Dto.ScoreDto>();
            CreateMap<Model.Season, Dto.SeasonDto>();
            CreateMap<Model.Series, Dto.SeriesDto>();

        }
    }
}
