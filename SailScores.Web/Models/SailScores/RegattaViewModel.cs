using System;

namespace SailScores.Web.Models.SailScores
{
    public class RegattaViewModel : Core.Model.Regatta
    {
        private Guid _seasonId;
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
