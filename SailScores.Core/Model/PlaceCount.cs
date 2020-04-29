using SailScores.Database.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SailScores.Core.Model
{
    public class PlaceCount
    {
        public int? Place { get; set; }
        public string Code { get; set; }
        public int? Count { get; set; }
    }
}
