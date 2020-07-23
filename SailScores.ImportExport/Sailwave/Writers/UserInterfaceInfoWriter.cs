using System;
using System.Collections.Generic;
using SailScores.ImportExport.Sailwave.Elements;
using SailScores.ImportExport.Sailwave.Elements.File;

namespace SailScores.ImportExport.Sailwave.Writers
{
    public class UserInterfaceInfoWriter
    {

        public static FileRow GetRow(UserInterfaceInfo info)
        {
            var returnRow = new FileRow
            {
                Name = "ui",
                Value = GetUiValue(info)
            };

            return returnRow;

        }

        private static string GetUiValue(UserInterfaceInfo info)
        {
            // 15 pipe separated values
            //"1|0|0|0|0|0|0|0|0|0|0|0|0|0|0"

            var strings = new List<string>();

            strings.Add(Utilities.BoolToOneZero(!OrAllTheValues(info)));

            strings.Add(Utilities.BoolToOneZero(info.MultipleScoringSystems));
            strings.Add(Utilities.BoolToOneZero(info.ExtraCompetitorFields));
            strings.Add(Utilities.BoolToOneZero(info.CompetitorAliasing));
            strings.Add(Utilities.BoolToOneZero(info.SplitStarts));
            strings.Add(Utilities.BoolToOneZero(info.QualificationProfile));
            strings.Add(Utilities.BoolToOneZero(info.RaceWeightings));
            strings.Add(Utilities.BoolToOneZero(info.MedalRace));
            strings.Add(Utilities.BoolToOneZero(info.AppendixLE));
            strings.Add(Utilities.BoolToOneZero(info.NonStandardRaceTieOptions));
            strings.Add(Utilities.BoolToOneZero(info.NonStandardSeriesTieOptions));
            strings.Add(Utilities.BoolToOneZero(info.NonStandardAccumulationOfPoints));
            strings.Add(Utilities.BoolToOneZero(info.WindIndexedRatings));
            strings.Add(Utilities.BoolToOneZero(info.HighPointScoring));
            strings.Add(Utilities.BoolToOneZero(info.NationBasedPublishingTemplates));

            return String.Join("|", strings);
        }

        private static bool OrAllTheValues(UserInterfaceInfo info)
        {
            return info.MultipleScoringSystems
                || info.ExtraCompetitorFields
                || info.CompetitorAliasing
                || info.SplitStarts
                || info.QualificationProfile
                || info.RaceWeightings
                || info.MedalRace
                || info.AppendixLE
                || info.NonStandardRaceTieOptions
                || info.NonStandardSeriesTieOptions
                || info.NonStandardAccumulationOfPoints
                || info.WindIndexedRatings
                || info.HighPointScoring
                || info.NationBasedPublishingTemplates;
        }
    }
}
