using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SailScores.Identity.Entities;

namespace SailScores.Web.Data
{
    public class SailScoresIdentityContext : IdentityDbContext<ApplicationUser>
    {
        public SailScoresIdentityContext(DbContextOptions<SailScoresIdentityContext> options)
            : base(options)
        {
        }
    }
}
