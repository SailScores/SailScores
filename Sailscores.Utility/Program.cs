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
            // get filestream
            string fullpath = GetImportFilePath();
            // import filestream
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
            throw new NotImplementedException();
        }

    }
}
