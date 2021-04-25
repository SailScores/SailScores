using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public interface ISpeechService
    {
        Task<string> GetToken();
        string GetRegion();
    }
}