using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Sailscores.Web.Data
{
    public class SailscoresIdentityContext : IdentityDbContext
    {
        public SailscoresIdentityContext(DbContextOptions<SailscoresIdentityContext> options)
            : base(options)
        {
        }
    }
}
