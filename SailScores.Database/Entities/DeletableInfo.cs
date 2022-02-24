using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Database.Entities
{

#pragma warning disable CA2227 // Collection properties should be read only
    public class DeletableInfo
    {
        public Guid Id { get; set; }

        public bool IsDeletable { get; set; }

        public string Reason { get; set; }
    }

#pragma warning restore CA2227 // Collection properties should be read only
}
