using SailScores.ImportExport.Sailwave.Parsers;
using System;
using System.IO;
using SwObjects = SailScores.ImportExport.Sailwave.Elements;
using SsObjects = SailScores.Core.Model;
using Microsoft.Extensions.DependencyInjection;
using SailScores.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SailScores.Core.Mapping;
using System.Reflection;
using AutoMapper;
using SailScores.Api.Services;
using SailScores.Core.Extensions;

namespace SailScores.Utility
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
            services.RegisterCoreSailScoresServices();
            services.AddDbContext<SailScoresContext>(options =>
                options.UseSqlServer(
                    config.GetConnectionString("DefaultConnection")));
            services.AddDbContext<ISailScoresContext, SailScoresContext>();
            services.AddTransient<ILocalServiceImporter, LocalServiceImporter>();
            services.AddTransient<IRestImporter, RestImporter>();
            services.AddTransient<ISailScoresApiClient, SailScoresApiClient>();
            services.AddSingleton<ISettings, ConsoleSettings>();
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
            Console.WriteLine("SailScores Primitive Utilities");
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


        private static void StartDbImport()
        {
            // get file path
            string fullpath = GetImportFilePath();
            // import file
            var series = ImportSWFile(fullpath);

            var localServiceImporter = _serviceProvider.GetService<ILocalServiceImporter>();
            localServiceImporter.WriteSwSeriesToSS(series);
        }

        private static void StartWebApiImport()
        {
            // get file path
            string fullpath = GetImportFilePath();
            // import file
            var series = ImportSWFile(fullpath);

            Console.WriteLine($"About to import a series with {series.Races.Count} race(s).");

            var restImporter = _serviceProvider.GetService<IRestImporter>();
            var importTask = restImporter.WriteSwSeriesToSS(series);
            importTask.Wait();
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
            }
            else
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

    }
}
