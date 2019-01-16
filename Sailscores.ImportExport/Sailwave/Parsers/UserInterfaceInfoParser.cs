using System.Collections.Generic;
using System.Linq;
using SailScores.ImportExport.Sailwave.Elements;
using SailScores.ImportExport.Sailwave.Elements.File;

namespace SailScores.ImportExport.Sailwave.Parsers
{
    public class UserInterfaceInfoParser
    {

        // rows should come in filtered for a single ScoringSystem.
        public static UserInterfaceInfo GetUiInfo(IEnumerable<FileRow> rows)
        {
            UserInterfaceInfo returnInfo;
            var row = rows.First(r => r.RowType == RowType.UserInterface);
            returnInfo = ParseRow(row);

            return returnInfo;
        }

        private static UserInterfaceInfo ParseRow(FileRow row)
        {
            var elements = row.Value.Split('|');
            return new UserInterfaceInfo
            {
                ExtraCompetitorFields = Utilities.GetBool(elements[2]) ?? false,
                HighPointScoring = Utilities.GetBool(elements[13]) ?? false,
                NonStandardRaceTieOptions = Utilities.GetBool(elements[9]) ?? false,
                NonStandardSeriesTieOptions = Utilities.GetBool(elements[10]) ?? false,
                QualificationProfile = Utilities.GetBool(elements[5]) ?? false,
                NonStandardAccumulationOfPoints = Utilities.GetBool(elements[11]) ?? false,
                SplitStarts = Utilities.GetBool(elements[4]) ?? false,
                AppendixLE = Utilities.GetBool(elements[8]) ?? false,
                MedalRace = Utilities.GetBool(elements[7]) ?? false,
                MultipleScoringSystems = Utilities.GetBool(elements[1]) ?? false,
                CompetitorAliasing = Utilities.GetBool(elements[3]) ?? false,
                WindIndexedRatings = Utilities.GetBool(elements[12]) ?? false,
                RaceWeightings = Utilities.GetBool(elements[6]) ?? false,
                NationBasedPublishingTemplates = Utilities.GetBool(elements[14]) ?? false
            };

        }
    }
}
