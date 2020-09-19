using AutoMapper;
using SailScores.Core.Mapping;
using System;
using System.Collections.Generic;
using System.Text;
using SailScores.Web.Mapping;

namespace SailScores.Test.Unit.Utilities
{
    public class MapperBuilder
    {
        public static IMapper GetSailScoresMapper()
        {

            var config = new MapperConfiguration(opts =>
            {
                opts.AddProfile(new DbToModelMappingProfile());
                opts.AddProfile(new DbToDtoMappingProfile());
                opts.AddProfile(new ModelToDtoMappingProfile());
                opts.AddProfile(new ToViewModelMappingProfile());
            });


            return config.CreateMapper();

        }
    }
}
