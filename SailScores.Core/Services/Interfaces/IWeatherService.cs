using SailScores.Core.Model;
using System;
using System.Threading.Tasks;

namespace SailScores.Core.Services
{
    public interface IWeatherService
    {
        Task<Weather> GetCurrentWeatherAsync(decimal latitude, decimal longitude);
    }
}