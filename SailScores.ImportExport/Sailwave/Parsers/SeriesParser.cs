﻿using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using SailScores.ImportExport.Sailwave.Csv;
using SailScores.ImportExport.Sailwave.Elements;
using SailScores.ImportExport.Sailwave.Elements.File;

namespace SailScores.ImportExport.Sailwave.Parsers
{
    public class SeriesParser : Parser<Series>
    {
        public static Series GetSeries(StreamReader reader)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false
            };

            var csv = new CsvReader(reader, config);
            csv.Context.RegisterClassMap<ColumnMapFromCsv>();
            var rows = csv.GetRecords<FileRow>().ToList();
            return GetSeries(rows);
        }

        public static Series GetSeries(IEnumerable<FileRow> rows)
        {
            var newSeries = new Series();

            var parser = new SeriesDetailsParser();
            var details = parser.LoadType(rows);

            newSeries.Details = details;
            newSeries.ScoringSystems = ScoringSystemSetParser.GetScoringSystems(rows);
            newSeries.UserInterface = UserInterfaceInfoParser.GetUiInfo(rows);
            newSeries.Competitors = CompetitorSetParser.GetCompetitors(rows);
            newSeries.Races = RaceSetParser.GetRaces(rows, newSeries.Competitors);
            newSeries.Columns = ColumnSetParser.GetColumns(rows);

            return newSeries;
        }

    }
}
