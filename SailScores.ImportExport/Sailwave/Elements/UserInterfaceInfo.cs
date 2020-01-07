using System.Text;

namespace SailScores.ImportExport.Sailwave.Elements
{
    public class UserInterfaceInfo
    {
        public bool ExtraCompetitorFields { get; set; }
        public bool HighPointScoring { get; set; }
        public bool NonStandardRaceTieOptions { get; set; }
        public bool NonStandardSeriesTieOptions { get; set; }
        public bool QualificationProfile { get; set; }
        public bool NonStandardAccumulationOfPoints { get; set; }
        public bool SplitStarts { get; set; }
        public bool AppendixLE { get; set; }
        public bool MedalRace { get; set; }
        public bool MultipleScoringSystems { get; set; }
        public bool CompetitorAliasing { get; set; }
        public bool WindIndexedRatings { get; set; }
        public bool RaceWeightings { get; set; }
        public bool NationBasedPublishingTemplates { get; set; }

        public string GetStringValue()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(ToIntString(AreAllFalse()) + "|");
            sb.Append(ToIntString(MultipleScoringSystems) + "|");
            sb.Append(ToIntString(ExtraCompetitorFields) + "|");
            sb.Append(ToIntString(CompetitorAliasing) + "|");
            sb.Append(ToIntString(SplitStarts) + "|"); //5
            sb.Append(ToIntString(QualificationProfile) + "|");
            sb.Append(ToIntString(RaceWeightings) + "|");
            sb.Append(ToIntString(MedalRace) + "|");
            sb.Append(ToIntString(AppendixLE) + "|");
            sb.Append(ToIntString(NonStandardRaceTieOptions) + "|");//10
            sb.Append(ToIntString(NonStandardSeriesTieOptions) + "|");
            sb.Append(ToIntString(NonStandardAccumulationOfPoints) + "|");
            sb.Append(ToIntString(WindIndexedRatings) + "|");
            sb.Append(ToIntString(HighPointScoring) + "|");
            sb.Append(ToIntString(NationBasedPublishingTemplates));

            return sb.ToString();
        }

        private bool AreAllFalse()
        {
            return !
            (ExtraCompetitorFields
             || HighPointScoring
             || NonStandardRaceTieOptions
             || NonStandardSeriesTieOptions
             || QualificationProfile
             || NonStandardAccumulationOfPoints
             || SplitStarts
             || AppendixLE
             || MedalRace
             || MultipleScoringSystems
             || CompetitorAliasing
             || WindIndexedRatings
             || RaceWeightings
             || NationBasedPublishingTemplates);
        }

        private string ToIntString(bool b)
        {
            return b ? "1" : "0";
        }
        //"ui","1|0|0|0|0|0|0|0|0|0|0|0|0|0|0","",""

    }
}