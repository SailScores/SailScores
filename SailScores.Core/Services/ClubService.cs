using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace SailScores.Core.Services
{
    public class ClubService : IClubService
    {
        private readonly ISailScoresContext _dbContext;

        public ClubService(ISailScoresContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IEnumerable<string> GetClubs()
        {

            return new string[] { "Club1", "Club2" };
        }
    }
}
