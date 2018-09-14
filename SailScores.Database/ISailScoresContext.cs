using Microsoft.EntityFrameworkCore;
using SailScores.Database.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SailScores.Database
{
    public interface ISailScoresContext : IDisposable {
        DbSet<Club> Clubs { get; set; }
    }
}
