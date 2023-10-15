using AutoMapper;
using System.Linq;
using Db = SailScores.Database.Entities;

namespace SailScores.Core.Mapping
{
    public class DbToModelMappingProfile : Profile
    {
        public DbToModelMappingProfile()
        {
            CreateMap<Db.Club, Model.Club>()
                .ReverseMap();
            CreateMap<Db.WeatherSettings, Model.WeatherSettings>()
                .ReverseMap()
                .ForMember(d => d.Id, o => o.Ignore());

            CreateMap<Db.Competitor, Model.Competitor>()
                .ForMember(d => d.Fleets, o => o.MapFrom(s => s.CompetitorFleets.Select(f => f.Fleet).ToList()))
                .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActive ?? true))
                .ReverseMap();

            CreateMap<Db.BoatClass, Model.BoatClass>()
                .ForMember(d => d.Club, o => o.Ignore())
                .ReverseMap();
            CreateMap<Db.Series, Model.Series>()
                .ForMember(d => d.Races, o => o.MapFrom(s => s.RaceSeries.Select(rs => rs.Race).ToList()))
                .ForMember(d => d.Results, o => o.Ignore())
                .ForMember(d => d.FlatResults, o => o.Ignore())
                .ForMember(d => d.Competitors, o => o.MapFrom(s =>
                        s.RaceSeries
                        .SelectMany(rs => rs.Race.Scores
                            .Select(r => r.Competitor)).Distinct().ToList()))
                .ForMember(d => d.ShowCompetitorClub, o => o.Ignore())
                .ForMember(d => d.ExcludeFromCompetitorStats, o => o.MapFrom(s => (s.ExcludeFromCompetitorStats ?? false)))
                .ReverseMap()
                .ForMember(d => d.RaceSeries, o => o.Ignore());
            CreateMap<Db.Fleet, Model.Fleet>()
                .ForMember(d => d.Competitors, o => o.MapFrom(s => s.CompetitorFleets.Select(cf => cf.Competitor).ToList()))
                .ForMember(d => d.BoatClasses, o => o.MapFrom(s => s.FleetBoatClasses.Select(fbc => fbc.BoatClass).ToList()))
                .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActive ?? true))
                .ReverseMap();
            CreateMap<Db.Race, Model.Race>()
                .ForMember(d => d.Series, o => o.MapFrom(s => s.SeriesRaces.Select(f => f.Series).ToList()))
                .ForMember(d => d.Season, o => o.Ignore())
                .ReverseMap()
                .ForMember(d => d.SeriesRaces, o => o.Ignore());
            CreateMap<Db.Score, Model.Score>()
                .ForMember(d => d.Competitor, o => o.Ignore())
                .ReverseMap();

            CreateMap<Db.Regatta, Model.Regatta>()
                .ForMember(d => d.Series, o => o.MapFrom(s => s.RegattaSeries.Select(rs => rs.Series).ToList()))
                .ForMember(d => d.Fleets, o => o.MapFrom(s => s.RegattaFleet.Select(rs => rs.Fleet).ToList()))
                .ForMember(d => d.PreferAlternateSailNumbers, o => o.MapFrom(s => s.PreferAlternateSailNumbers ?? false))
                .ForMember(d => d.Documents, o => o.Ignore())
                .ReverseMap();
            CreateMap<Db.Announcement, Model.Announcement>()
                .ReverseMap();
            CreateMap<Db.Document, Model.Document>()
                .ReverseMap();

            CreateMap<Db.Season, Model.Season>()
                .ReverseMap();

            CreateMap<Db.ScoringSystem, Model.ScoringSystem>()
                .ForMember(d => d.InheritedScoreCodes, o => o.Ignore())
                .ReverseMap();
            CreateMap<Db.ScoreCode, Model.ScoreCode>()
                .ForMember(d => d.ClubId, o => o.Ignore())
                .ReverseMap();
            CreateMap<Db.ClubRequest, Model.ClubRequest>()
                .ReverseMap();
            CreateMap<Db.Weather, Model.Weather>()
                .ReverseMap();

            CreateMap<Db.CompetitorRankStats, Model.PlaceCount>();

            CreateMap<Db.CompetitorStatsSummary, Model.CompetitorSeasonStats>();
        }
    }
}
