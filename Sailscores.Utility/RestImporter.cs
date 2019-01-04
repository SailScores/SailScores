using SailScores.ImportExport.Sailwave.Parsers;
using System;
using System.IO;
using SwObjects = SailScores.ImportExport.Sailwave.Elements;
using SsObjects = SailScores.Core.Model;
using System.Collections.Generic;
using SailScores.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using SailScores.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SailScores.Core.Mapping;
using System.Reflection;
using AutoMapper;
using System.Threading.Tasks;
using System.Linq;
using SailScores.ApiClient.Services;

namespace SailScores.Utility
{
    class RestImporter : IRestImporter
    {
        private readonly ISailScoresApiClient _apiClient;

        public RestImporter(
            ISailScoresApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public void WriteSwSeriesToSS(SwObjects.Series series)
        {
            Console.WriteLine($"About to import a series with {series.Races.Count} race(s).");

            // Authenticate

            try
            {
                _apiClient.GetClubsAsync().Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"There was an exception: {ex.ToString()}");
            }

            // Get club list

            // Refer to LocalServiceImporter for rough structure.

        }
    }
}
