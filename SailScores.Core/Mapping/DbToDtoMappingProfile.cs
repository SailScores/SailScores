using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dto = SailScores.Api.Dtos;
using Db = SailScores.Database.Entities;

namespace SailScores.Core.Mapping
{
    // This class isn't used too much as of writing: Most things go from db object
    // to Model/core object, then to DTO. But for competitor saving, and possibly
    // other future uses, we're going directly from DTO to db/EF object.
    public class DbToDtoMappingProfile : Profile
    {
        public DbToDtoMappingProfile()
        {
            CreateMap<Dto.CompetitorDto, Db.Competitor>()
                .ForMember(d => d.BoatClass, o => o.Ignore())
                .ForMember(d => d.CompetitorFleets, o => o.Ignore())
                .ForMember(d => d.Scores, o => o.Ignore());

        }
    }
}
