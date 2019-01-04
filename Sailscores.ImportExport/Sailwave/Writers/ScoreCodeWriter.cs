using System;
using System.Collections.Generic;
using SailScores.ImportExport.Sailwave.Elements;
using SailScores.ImportExport.Sailwave.Elements.File;

namespace SailScores.ImportExport.Sailwave.Writers
{
    public class ScoreCodeWriter
    {
        public static FileRow GetRow(ScoreCode code)
        {
            var returnRow = new FileRow
            {
                Name = "scrcode",
                Value = GetScoreCodeValue(code)
            };
            
            return returnRow;
        }

        private static string GetScoreCodeValue(ScoreCode code)
        {
            // 17 pipe separated values
            //"RET|Score like|DNF|Yes|Yes||spare|spare|spare|spare|spare|Yes|No|No|5||Retired"
            string spare = "spare";

            var strings = new List<string>();

            strings.Add( code.Code );  // 0
            strings.Add( code.Method );
            strings.Add( code.Value );
            strings.Add( Utilities.BoolToYesNo( code.Discardable) );
            strings.Add( Utilities.BoolToYesNo( code.CameToStartArea)); //4
            strings.Add( string.Empty );
            strings.Add( spare );
            strings.Add( spare );
            strings.Add( spare );//8
            strings.Add( spare );
            strings.Add( spare );
            strings.Add(Utilities.BoolToYesNo(code.Started)); // 11
            strings.Add(Utilities.BoolToYesNo(code.Finished));
            strings.Add(Utilities.BoolToYesNo(code.RuleA6d2Applies));
            strings.Add(code.ScoringSystemId.ToString());
            strings.Add(code.Format);
            strings.Add(code.Description); //16
            
            return String.Join("|", strings);
        }
    }
}
