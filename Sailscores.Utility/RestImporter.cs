using System;
using SwObjects = SailScores.ImportExport.Sailwave.Elements;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            _boatClass = await GetBoatClass();
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
                var guid = await _apiClient.SaveClub(club);
                club.Id = guid;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Oh Noes! There was an exception: {ex.ToString()}");
                throw;
            }

            return club;
        }

        private async Task<BoatClassDto> GetBoatClass()
        {
            var boatClasses = await _apiClient.GetBoatClassesAsync(_club.Id);
            Console.WriteLine($"There are {boatClasses.Count} boat classes already in the database.");
            Console.Write("Would you like to use one of those? (Y / N)");
            var result = Console.ReadLine();
            if (result.StartsWith("Y", StringComparison.InvariantCultureIgnoreCase))
            {
                return SelectExistingClass(boatClasses);
            }
            else
            {
                return await MakeNewBoatClass();
            }
        }

        private BoatClassDto SelectExistingClass(List<BoatClassDto> classes)
        {
            int i = 1;
            foreach (var boatClass in classes)
            {
                Console.WriteLine($"{i++} - {boatClass.Name}");
            }
            int result = 0;
            while (result == 0)
            {
                Console.Write("Enter a number of a class from above > ");
                var input = Console.ReadLine();
                Int32.TryParse(input, out result);
            }

            return classes[result - 1];
        }


        private async Task<BoatClassDto> MakeNewBoatClass()
        {
            Console.Write("Enter the new class name > ");
            var className = Console.ReadLine().Trim();

            var boatClass = new BoatClassDto
            {
                Name = className,
                ClubId = _club.Id
            };

            try
            {
                var guid = await _apiClient.SaveBoatClass(boatClass);
                boatClass.Id = guid;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Oh Noes! There was an exception: {ex.ToString()}");
                throw;
            }

            return boatClass;
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
