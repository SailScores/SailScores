using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.SailScores
{
    public class SeriesWithOptionsViewModel : Core.Model.Series
    {
        public IEnumerable<Season> SeasonOptions { get; set; }

        public IList<ScoringSystem> ScoringSystemOptions { get; set; }

        private Guid _seasonId;

        [Required]
        public Guid SeasonId
        {
            get
            {
                if (this.Season != null)
                {
                    return this.Season.Id;
                }
                return _seasonId;
            }
            set
            {
                _seasonId = value;
            }

        }
    }
}
