using AutoMapper;
using SailScores.Core.Mapping;
using System;
using System.Collections.Generic;
using System.Text;

namespace SailScores.Test.Unit.Utilities
{
    public class MapperBuilder
    {
        public static IMapper GetSailScoresMapper()
        {

            var config = new MapperConfiguration(opts =>
            {
                opts.AddProfile(new DbToModelMappingProfile());
            });


            return config.CreateMapper();

        }
    }
}
