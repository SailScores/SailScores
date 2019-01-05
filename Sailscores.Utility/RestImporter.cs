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
using SailScores.Api.Services;
using SailScores.Api.Dtos;

namespace SailScores.Utility
{
    class RestImporter : IRestImporter
    {
        private readonly ISailScoresApiClient _apiClient;

        private ClubDto _club;
        private BoatClassDto _boatClass;
        private FleetDto _fleet;
        private SeriesDto _series;
        private IList<CompetitorDto> _competitors;
        private int _year;

        public RestImporter(
            ISailScoresApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task WriteSwSeriesToSS(SwObjects.Series series)
        {
            _club = await GetClub();
            _boatClass = GetBoatClass();
            _fleet = GetFleet();
            _year = GetYear();
            _series = MakeSeries(series);
            _competitors = GetCompetitors(series);
            SaveRaces(series);
        }

        private async Task<ClubDto> GetClub()
        {
            var clubs = await _apiClient.GetClubsAsync();
            Console.WriteLine($"There are {clubs.Count} clubs already in the database.");
            Console.Write("Would you like to use one of those? (Y / N)");
            var result = Console.ReadLine();
            if (result.StartsWith("Y", StringComparison.InvariantCultureIgnoreCase))
            {
                return SelectExistingClub(clubs);
            }
            else
            {
                return await MakeNewClub();
            }
        }

        private ClubDto SelectExistingClub(List<ClubDto> clubs)
        {
            int i = 1;
            foreach (var club in clubs)
            {
                Console.WriteLine($"{i++} - {club.Initials} : {club.Name}");
            }
            int result = 0;
            while (result == 0)
            {
                Console.Write("Enter a number of a club from above > ");
                var input = Console.ReadLine();
                Int32.TryParse(input, out result);
            }

            return clubs[result-1];
        }

        private async Task<ClubDto> MakeNewClub()
        {
            Console.Write("Enter the new club name > ");
            var clubName = Console.ReadLine().Trim();
            Console.Write("Enter the club initials > ");
            var clubInitials = Console.ReadLine().Trim();

            var club = new ClubDto
            {
                Initials = clubInitials,
                Name = clubName
            };

            try
            {
                var createTask = await _apiClient.SaveClub(club);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Oh Noes! There was an exception: {ex.ToString()}");
                throw;
            }

            return club;
        }

        private BoatClassDto GetBoatClass()
        {
            throw new NotImplementedException();
        }

        private FleetDto GetFleet()
        {
            throw new NotImplementedException();
        }

        private int GetYear()
        {
            throw new NotImplementedException();
        }
        private SeriesDto MakeSeries(SwObjects.Series series)
        {
            throw new NotImplementedException();
        }

        private IList<CompetitorDto> GetCompetitors(SwObjects.Series series)
        {
            throw new NotImplementedException();
        }

        private void SaveRaces(SwObjects.Series series)
        {
            throw new NotImplementedException();
        }


    }
}
