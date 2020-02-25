using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public interface IAppVersionService
    {
        string Version { get; }
    }
}
