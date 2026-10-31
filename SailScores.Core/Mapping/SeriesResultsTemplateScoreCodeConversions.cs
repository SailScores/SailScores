using System;
using System.Collections.Generic;
using System.Linq;
using SailScores.Core.Model;
using Db = SailScores.Database.Entities;

namespace SailScores.Core.Mapping;

public static class SeriesResultsTemplateScoreCodeConversions
{
    public static List<SeriesResultsTemplateScoreCode> ToModels(IEnumerable<Db.SeriesResultsTemplateScoreCode> codes) =>
        (codes ?? Enumerable.Empty<Db.SeriesResultsTemplateScoreCode>())
            .Select(c => new SeriesResultsTemplateScoreCode
            {
                Code = c.Code,
                Display = c.Display,
                Faded = c.Faded
            })
            .OrderBy(c => c.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static Db.SeriesResultsTemplateScoreCode ToDb(SeriesResultsTemplateScoreCode code, Guid templateId) => new()
    {
        Id = Guid.NewGuid(),
        SeriesResultsTemplateId = templateId,
        Code = code.Code,
        Display = code.Display,
        Faded = code.Faded
    };
}
