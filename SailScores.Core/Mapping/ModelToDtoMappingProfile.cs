using AutoMapper;
using System;
using System.Linq;
using Dto = SailScores.Api.Dtos;

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
                .ReverseMap()
                .ForMember(d => d.BoatClass, o => o.Ignore());

            CreateMap<Model.Fleet, Dto.FleetDto>()
                .ForMember(d => d.BoatClassIds, o => o.MapFrom(s => s.BoatClasses.Select(c => c.Id)))
                .ForMember(d => d.CompetitorIds, o => o.MapFrom(s => s.Competitors.Select(c => c.Id)))
                .ReverseMap();
            CreateMap<Model.Race, Dto.RaceDto>()
                .ForMember(d => d.ScoreIds, o => o.MapFrom(s => s.Scores.Select(c => c.Id)))
                .ForMember(d => d.SeriesIds, o => o.MapFrom(s => s.Series.Select(c => c.Id)))
                .ForMember(d => d.RegattaId, o => o.Ignore());
            CreateMap<Model.ScoreCode, Dto.ScoreCodeDto>();
            CreateMap<Model.Score, Dto.ScoreDto>()
                .ReverseMap();
            CreateMap<Model.Season, Dto.SeasonDto>()
                .ForMember(d => d.SeriesIds, o => o.MapFrom(s => s.Series.Select(c => c.Id)))
                .ReverseMap()
                .ForMember(d => d.Series, o => o.Ignore());
            CreateMap<Model.Series, Dto.SeriesDto>()
                .ForMember(d => d.RaceIds, o => o.MapFrom(s => s.Races.Select(c => c.Id)))
                .ReverseMap();

            CreateMap<Model.OpenWeatherMap.CurrentWeatherResponse, Model.Weather>()
                .ForMember(d => d.Description,
                    o => o.MapFrom(
                        s => String.Join(", ", s.Weather.Select(w => w.Main)) + "; " +
                       String.Join(", ", s.Weather.Select(w => w.Description))))
                .ForMember(d => d.Icon, o => o.MapFrom(s => s.Weather.First().Icon))
                .ForMember(d => d.TemperatureDegreesKelvin,
                    o => o.MapFrom(s => s.Main.TemperatureDegreesKelvin))
                .ForMember(d => d.WindSpeedMeterPerSecond,
                    o => o.MapFrom(s => s.Wind.Speed))
                .ForMember(d => d.WindDirectionDegrees,
                    o => o.MapFrom(s => s.Wind.Degrees))
                .ForMember(d => d.WindGustMeterPerSecond,
                    o => o.MapFrom(s => s.Wind.Gust))
                .ForMember(d => d.Humidity,
                    o => o.MapFrom(s => s.Main.HumidityPercent))
                .ForMember(d => d.CloudCoverPercent,
                    o => o.MapFrom(s => s.Clouds.CoverPercent))
                .ForMember(d => d.CreatedDate, o => o.MapFrom(s => DateTime.Now))
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.TemperatureString, o => o.Ignore())
                .ForMember(d => d.WindSpeedString, o => o.Ignore())
                .ForMember(d => d.WindDirectionString, o => o.Ignore())
                .ForMember(d => d.WindGustString, o => o.Ignore());


            CreateMap<Model.Weather, Dto.WeatherDto>();


        }
    }
}
