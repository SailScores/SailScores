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
        private SeasonDto _season;
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
            _fleet = await GetFleet();
            _year = GetYear();
            _season = await GetSeason();
            _series = await MakeSeries(series);
            _competitors = await GetCompetitors(series);
            await SaveRaces(series);
        }
        
        private async Task<ClubDto> GetClub()
        {
            var clubs = await _apiClient.GetClubsAsync();
            if (clubs.Count > 0)
            {
                Console.WriteLine($"There are {clubs.Count} clubs already in the database.");
                Console.Write("Would you like to use one of those? (Y / N) ");
                var result = Console.ReadLine();
                if (result.StartsWith("Y", StringComparison.InvariantCultureIgnoreCase))
                {
                    return SelectExistingClub(clubs);
                }
            }
            return await MakeNewClub();
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
            if (boatClasses.Count > 0)
            {
                Console.WriteLine($"There are {boatClasses.Count} boat classes already in the database.");
                Console.Write("Would you like to use one of those? (Y / N) ");
                var result = Console.ReadLine();
                if (result.StartsWith("Y", StringComparison.InvariantCultureIgnoreCase))
                {
                    return SelectExistingClass(boatClasses);
                }
            }
            return await MakeNewBoatClass();
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


        private async Task<FleetDto> GetFleet()
        {
            var fleets = await _apiClient.GetFleetsAsync(_club.Id);
            if (fleets.Count > 0)
            {
                Console.WriteLine($"There are {fleets.Count} fleets already in the database.");
                Console.Write("Would you like to use one of those? (Y / N) ");
                var result = Console.ReadLine();
                if (result.StartsWith("Y", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (fleets.Count > 1)
                    {
                        return SelectExistingFleet(fleets);
                    }
                    else
                    {
                        return fleets[0];
                    }
                }
            }
            return await MakeNewFleet();
        }

        private FleetDto SelectExistingFleet(List<FleetDto> fleets)
        {
            int i = 1;
            foreach (var fleet in fleets)
            {
                Console.WriteLine($"{i++} - {fleet.Name}");
            }
            int result = 0;
            while (result == 0)
            {
                Console.Write("Enter a number of a fleet from above > ");
                var input = Console.ReadLine();
                Int32.TryParse(input, out result);
            }

            return fleets[result - 1];
        }


        private async Task<FleetDto> MakeNewFleet()
        {
            Console.Write("Enter the new fleet name > ");
            var className = Console.ReadLine().Trim();
            Console.Write("Enter the new fleet short name / abbreviation > ");
            var shortName = Console.ReadLine().Trim();

            var fleet = new FleetDto
            {
                Name = className,
                ShortName = shortName,
                ClubId = _club.Id
            };

            try
            {
                var guid = await _apiClient.SaveFleet(fleet);
                fleet.Id = guid;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Oh Noes! There was an exception: {ex.ToString()}");
                throw;
            }

            return fleet;
        }

        private static int GetYear()
        {
            Console.Write("What year was this series? > ");
            var result = 0;
            while (result < 1900)
            {
                var input = Console.ReadLine();
                Int32.TryParse(input, out result);
            }
            return result;
        }

        private Task<SeasonDto> GetSeason()
        {
            var seasons = await _apiClient.GetSeasonsAsync(_club.Id);

            var 

        }

        private async Task<SeriesDto> MakeSeries(SwObjects.Series series)
        {
            Console.Write("What is the name of this series? > ");
            var result = Console.ReadLine();
            var seriesDto = new SeriesDto
            {
                ClubId = _club.Id,
                Name = result,
                SeasonId = _season.Id
            };

            try
            {
                var guid = await _apiClient.SaveSeries(seriesDto);
                seriesDto.Id = guid;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Oh Noes! There was an exception: {ex.ToString()}");
                throw;
            }
            return seriesDto;
        }

        private async Task<IList<CompetitorDto>> GetCompetitors(SwObjects.Series series)
        {
            throw new NotImplementedException();
        }

        private async Task SaveRaces(SwObjects.Series series)
        {
            throw new NotImplementedException();
        }


    }
}
