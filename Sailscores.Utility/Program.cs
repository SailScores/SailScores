using Sailscores.ImportExport.Sailwave.Parsers;
using System;
using System.IO;
using SwObjects = Sailscores.ImportExport.Sailwave.Elements;
using SsObjects = Sailscores.Core.Model;
using System.Collections.Generic;
using Sailscores.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Sailscores.Core.Services;
using Sailscores.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Sailscores.Core.Services;
using Sailscores.Core.Mapping;
using System.Reflection;
using AutoMapper;
using System.Threading.Tasks;
using System.Linq;

namespace Sailscores.Utility
{
    class Program
    {
        private static ServiceProvider _serviceProvider;

        static void Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json", true, true)
              .Build();

            var services = new ServiceCollection();
            services.RegisterCoreSailscoresServices();
            services.AddDbContext<SailscoresContext>(options =>
                options.UseSqlServer(
                    config.GetConnectionString("DefaultConnection")));
            services.AddDbContext<ISailscoresContext, SailscoresContext>();
            services.AddAutoMapper(
                new[] {
                    typeof(DbToModelMappingProfile).GetTypeInfo().Assembly
                });
            _serviceProvider = services.BuildServiceProvider();

            int option = 0;
            while (option != 9)
            {
                option = GetMainMenuChoice();
                ActOnMainMenuOption(option);
            }
        }

        private static int GetMainMenuChoice()
        {
            Console.WriteLine("Sailscores Primitive Utilities");
            Console.WriteLine();
            Console.WriteLine("1 - Import a Sailwave file direct to local service");
            Console.WriteLine("2 - Import a Sailwave file via WebApi");
            Console.WriteLine("9 - Exit");
            Console.WriteLine();
            var result = 0;
            while (result == 0)
            {
                Console.Write("Select an option > ");
                var input = Console.ReadLine();
                Int32.TryParse(input, out result);
            }
            return result;
        }

        private static void ActOnMainMenuOption(int option)
        {
            switch (option)
            {
                case 1:
                    StartDbImport();
                    break;
                case 2:
                    StartWebApiImport();
                    break;
                default:
                    break;
            }
        }

        private static void StartWebApiImport()
        {
            throw new NotImplementedException();
        }

        private static void StartDbImport()
        {
            // get file path
            string fullpath = GetImportFilePath();
            // import file
            var series = ImportSWFile(fullpath);
            // write to Db
            WriteSwSeriesToSS(series);

        }


        private static string GetImportFilePath()
        {
            Console.WriteLine("What's the full path for the sailwave file (.blw)?");
            Console.Write(" > ");
            string filepath;
            filepath = Console.ReadLine();
            if (System.IO.File.Exists(filepath))
            {
                return filepath;
            } else
            {
                return GetImportFilePath();
            }
        }

        private static SwObjects.Series ImportSWFile(string fileName)
        {
            using (var reader = new StreamReader(fileName))
            {
                return SeriesParser.GetSeries(reader);
            }
        }

        private static void WriteSwSeriesToSS(SwObjects.Series series)
        {
            Console.WriteLine($"About to import a series with {series.Races.Count} race(s).");
            SsObjects.Club club = GetClub();
            SaveSeriesToClub(series, club);

        }

        private static void SaveSeriesToClub(SwObjects.Series series, SsObjects.Club club)
        {
            //todo: figure out fleet
            //todo: figure out boatclass
            var boatClass = GetBoatClass(club);
            var ssSeries = MakeSeries(club);
            ssSeries.Races = MakeRaces(series, club, boatClass);

            var seriesService = _serviceProvider.GetService<ISeriesService>();
            try
            {
                var createTask = seriesService.SaveNewSeries(ssSeries, club);
                createTask.Wait();
                //createTask.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Oh Noes! There was an exception: {ex.ToString()}");
            }
        }

        private static IList<SsObjects.Race> MakeRaces(
            SwObjects.Series series,
            SsObjects.Club club,
            SsObjects.BoatClass boatClass)
        {
            var retList = new List<SsObjects.Race>();

            foreach (var swRace in series.Races)
            {
                var ssRace = new SsObjects.Race
                {
                    Name = swRace.Name,
                    Order = swRace.Rank,
                    ClubId = club.Id,
                    Date = DateTime.Today,
                };
                ssRace.Scores = MakeScores(swRace, series.Competitors, boatClass);

                retList.Add(ssRace);
            }
            return retList;
        }
        
        private static IList<SsObjects.Score> MakeScores(
            SwObjects.Race swRace,
            IEnumerable<SwObjects.Competitor> swCompetitors,
            SsObjects.BoatClass boatClass)
        {
            var retList = new List<SsObjects.Score>();
            foreach(var swScore in swRace.Results)
            {
                var ssScore = new SsObjects.Score
                {
                    Place = swScore.Place,
                    Code = swScore.Code
                };
                var swCompetitor = swCompetitors.Single(c => c.Id == swScore.CompetitorId);
                ssScore.Competitor = new SsObjects.Competitor
                {
                    Name = swCompetitor.HelmName,
                    SailNumber = swCompetitor.SailNumber,
                    BoatClassId = boatClass.Id
                };
                retList.Add(ssScore);
            }

            return retList;
        }

        private static SsObjects.Club GetClub()
        {
            IList<SsObjects.Club> existingClubs = GetExistingClubs();
            Console.WriteLine($"There are {existingClubs.Count} clubs already in the database.");
            Console.Write("Would you like to use one of those? (Y / N)");
            var result = Console.ReadLine();
            if(result.StartsWith("Y", StringComparison.InvariantCultureIgnoreCase))
            {
                return SelectExistingClub();
            } else
            {
                return MakeNewClub();
            }
        }

        private static SsObjects.BoatClass GetBoatClass(SsObjects.Club club)
        {
            IList<SsObjects.BoatClass> existingClasses = club.BoatClasses;
            Console.WriteLine($"There are {existingClasses.Count} classes already in this club.");
            Console.Write("Would you like to use one of those? (Y / N)");
            var result = Console.ReadLine();
            if (result.StartsWith("Y", StringComparison.InvariantCultureIgnoreCase))
            {
                return SelectExistingBoatClass(club);
            }
            else
            {
                return MakeNewBoatClass(club);
            }
        }

        private static SsObjects.Series MakeSeries(SsObjects.Club club)
        {
            Console.Write("What is the name of this series? > ");
            var result = Console.ReadLine();
            SsObjects.Series ssSeries = new SsObjects.Series
            {
                ClubId = club.Id,
                Name = result
            };
            return ssSeries;
        }


        private static IList<SsObjects.Club> GetExistingClubs()
        {
            var clubService = _serviceProvider.GetService<IClubService>();
            var clubTask = clubService.GetClubs(true);
            return clubTask.Result;
        }

        private static SsObjects.Club SelectExistingClub()
        {
            var clubs = GetExistingClubs();
            Dictionary<int, SsObjects.Club> clubDict = new
                Dictionary<int, SsObjects.Club>();
            int i = 1;
            foreach(var club in clubs)
            {
                clubDict.Add(i++, club);
            }
            foreach(var kvp in clubDict)
            {
                Console.WriteLine($"{kvp.Key} - {kvp.Value.Initials} : {kvp.Value.Name}");
            }
            int result = 0;
            while (result == 0)
            {
                Console.Write("Enter a number of a club from above > ");
                var input = Console.ReadLine();
                Int32.TryParse(input, out result);
            }

            return clubDict[result];
        }

        private static SsObjects.Club MakeNewClub()
        {
            // Get Name and initials:
            Console.Write("Enter the new club name > ");
            var clubName = Console.ReadLine().Trim();
            Console.Write("Enter the club initials > ");
            var clubInitials = Console.ReadLine().Trim();

            var clubService = _serviceProvider.GetService<IClubService>();

            SsObjects.Club club = new SsObjects.Club
            {
                Initials = clubInitials,
                Name = clubName
            };

            try
            {
                var createTask = clubService.SaveNewClub(club);
                createTask.Wait();
                //createTask.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Oh Noes! There was an exception: {ex.ToString()}");
            }

            return club;
        }


        private static SsObjects.BoatClass SelectExistingBoatClass(SsObjects.Club club)
        {
            var boatClasses = club.BoatClasses;
            Dictionary<int, SsObjects.BoatClass> classDict = new
                Dictionary<int, SsObjects.BoatClass>();
            int i = 1;
            foreach (var boatClass in boatClasses)
            {
                classDict.Add(i++, boatClass);
            }
            foreach (var kvp in classDict)
            {
                Console.WriteLine($"{kvp.Key} - {kvp.Value.Name}");
            }
            int result = 0;
            while (result == 0)
            {
                Console.Write("Enter a number of a class from above > ");
                var input = Console.ReadLine();
                Int32.TryParse(input, out result);
            }

            return classDict[result];
        }

        private static SsObjects.BoatClass MakeNewBoatClass(SsObjects.Club club)
        {
            // Get Name and initials:
            Console.Write("Enter the new class name > ");
            var className = Console.ReadLine().Trim();

            SsObjects.BoatClass boatClass = new SsObjects.BoatClass
            {
                Id = Guid.NewGuid(),
                Name = className,
                ClubId = club.Id
            };

            return boatClass;
        }
    }
}
