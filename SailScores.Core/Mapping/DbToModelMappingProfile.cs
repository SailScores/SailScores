using AutoMapper;
using System;
using System.Collections.Generic;
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
        }
    }
}
