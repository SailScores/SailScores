using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SailScores.Web.Data
{
    public class SailScoresIdentityContext : IdentityDbContext
    {
        public SailScoresIdentityContext(DbContextOptions<SailScoresIdentityContext> options)
            : base(options)
        {
        }
    }
}
