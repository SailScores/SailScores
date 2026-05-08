using SailScores.ImportExport.Sailwave.Parsers;
using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using SwObjects = SailScores.ImportExport.Sailwave.Elements;
using Microsoft.Extensions.DependencyInjection;
using SailScores.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SailScores.Core.Mapping;
using System.Reflection;
using AutoMapper;
using SailScores.Api.Services;
using SailScores.Core.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace SailScores.Utility
{
    class Program
    {
        private static Microsoft.Extensions.DependencyInjection.ServiceProvider _serviceProvider;

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
            Console.WriteLine("3 - Reproduce iCal URL import dates (ImportIcal)");
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
                case 3:
                    ReproduceIcalImportDates();
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


        private static void ReproduceIcalImportDates()
        {
            Console.WriteLine("Enter iCal URL:");
            Console.Write(" > ");
            var url = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(url))
            {
                Console.WriteLine("A URL is required.");
                return;
            }

            var seasonStart = PromptForDate("Enter season start date (yyyy-MM-dd)");
            var seasonEnd = PromptForDate("Enter season end date (yyyy-MM-dd)");
            if (seasonEnd < seasonStart)
            {
                Console.WriteLine("Season end date must be on or after season start date.");
                return;
            }

            var content = GetIcalContentFromUrl(url.Trim());
            var calendar = Ical.Net.Calendar.Load(content);
            var occurrences = GetSortedOccurrences(calendar, seasonStart, seasonEnd);
            var importedSeries = BuildImportedSeries(occurrences, seasonStart, seasonEnd);

            var json = JsonSerializer.Serialize(
                importedSeries,
                new JsonSerializerOptions { WriteIndented = true });

            Console.WriteLine();
            Console.WriteLine("Response-style series payload:");
            Console.WriteLine(json);
            Console.WriteLine($"Included events: {importedSeries.Count}");
        }

        private static DateTime PromptForDate(string prompt)
        {
            while (true)
            {
                Console.WriteLine(prompt);
                Console.Write(" > ");
                var input = Console.ReadLine();

                if (DateTime.TryParseExact(
                    input,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsed))
                {
                    return parsed;
                }

                Console.WriteLine("Invalid date. Please use yyyy-MM-dd.");
            }
        }

        private static string GetIcalContentFromUrl(string url)
        {
            using var client = new HttpClient();
            return client.GetStringAsync(url).GetAwaiter().GetResult();
        }

        private static List<Occurrence> GetSortedOccurrences(Ical.Net.Calendar calendar, DateTime seasonStart, DateTime seasonEnd)
        {
            var start = new CalDateTime(seasonStart);
            var end = new CalDateTime(seasonEnd);

            var occurrences = new List<Occurrence>();
            foreach (var evt in calendar.Events)
            {
                var eventOccurrences = evt.GetOccurrences(start)
                    .TakeWhile(o => o.Period.StartTime.Value <= end.Value);

                occurrences.AddRange(eventOccurrences);
            }

            return occurrences
                .OrderBy(o => o.Period.StartTime.Value)
                .ToList();
        }

        private static List<ImportedSeriesPreview> BuildImportedSeries(
            List<Occurrence> occurrences,
            DateTime seasonStart,
            DateTime seasonEnd)
        {
            var results = new List<ImportedSeriesPreview>();

            var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var currentBatchNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var occurrence in occurrences)
            {
                var evt = occurrence.Source as CalendarEvent;
                if (evt == null)
                {
                    continue;
                }

                var (startDate, endDate) = GetDatesFromOccurrence(occurrence);

                if (!IsWithinSeason(startDate, endDate, seasonStart, seasonEnd))
                {
                    continue;
                }

                var name = GenerateUniqueName(evt.Summary, startDate, existingNames, currentBatchNames);
                currentBatchNames.Add(name);

                results.Add(new ImportedSeriesPreview
                {
                    Name = name,
                    StartDate = startDate.ToString("yyyy-MM-dd"),
                    EndDate = endDate.ToString("yyyy-MM-dd")
                });
            }

            return results;
        }

        private static (DateOnly Start, DateOnly End) GetDatesFromOccurrence(Occurrence occurrence)
        {
            var evt = occurrence.Source as CalendarEvent;
            var dtStart = occurrence.Period.StartTime.Value;
            var dtEnd = occurrence.Period?.EndTime?.Value ?? dtStart;

            DateOnly startDate;
            DateOnly endDate;

            if (evt != null && evt.IsAllDay)
            {
                startDate = DateOnly.FromDateTime(dtStart);
                endDate = DateOnly.FromDateTime(dtEnd.AddDays(-1));
                if (endDate < startDate)
                {
                    endDate = startDate;
                }
            }
            else
            {
                startDate = DateOnly.FromDateTime(dtStart);
                endDate = DateOnly.FromDateTime(dtEnd);
                if (endDate < startDate)
                {
                    endDate = startDate;
                }
            }

            return (startDate, endDate);
        }

        private static bool IsWithinSeason(DateOnly startDate, DateOnly endDate, DateTime seasonStart, DateTime seasonEnd)
        {
            var seasonStartDate = DateOnly.FromDateTime(seasonStart);
            var seasonEndDate = DateOnly.FromDateTime(seasonEnd);
            return !(startDate < seasonStartDate || endDate > seasonEndDate);
        }

        private static string GenerateUniqueName(
            string baseName,
            DateOnly startDate,
            HashSet<string> existingNames,
            HashSet<string> currentBatchNames)
        {
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = "Untitled Series";
            }

            var name = baseName;
            if (existingNames.Contains(name) || currentBatchNames.Contains(name))
            {
                name = $"{baseName} {startDate:MMM dd}";
                if (existingNames.Contains(name) || currentBatchNames.Contains(name))
                {
                    var counter = 1;
                    var nameWithDate = name;
                    do
                    {
                        name = $"{nameWithDate} {counter}";
                        counter++;
                    }
                    while (existingNames.Contains(name) || currentBatchNames.Contains(name));
                }
            }

            return name;
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
            using var reader = new StreamReader(fileName);
            return SeriesParser.GetSeries(reader);
        }

        private class ImportedSeriesPreview
        {
            public string Name { get; set; }
            public string StartDate { get; set; }
            public string EndDate { get; set; }
        }

    }
}
