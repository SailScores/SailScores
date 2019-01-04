using System;
using SailScores.ImportExport.Sailwave.Elements;
using SailScores.ImportExport.Sailwave.Elements.File;

namespace SailScores.ImportExport.Sailwave.Parsers
{
    public class ScoreCodeParser
    {

        // rows should come in filtered for a single race.
        public static ScoreCode GetCode(FileRow row)
        {
            if (!row.Name.Equals("scrcode", StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
            ScoreCode code = new ScoreCode();
            var elements = row.Value.Split('|');
            code.Code = elements[0];
            code.Method = elements[1];
            code.Value = elements[2];
            code.Discardable = Utilities.GetBool(elements[3]) ?? false;
            code.CameToStartArea = Utilities.GetBool(elements[4]) ?? false;
            code.Started = Utilities.GetBool(elements[11]) ?? false;
            code.Finished = Utilities.GetBool(elements[12]) ?? false;
            code.RuleA6d2Applies = Utilities.GetBool(elements[13]) ?? false;
            code.ScoringSystemId = Utilities.GetInt(elements[14]) ?? 1;
            code.Format = elements[15];
            code.Description = elements[16];

            return code;
        }
    }
}
