using SailScores.Core.Model;
using System;
using System.Collections.Generic;

namespace SailScores.Web.Models.SailScores
{
    public class AnnouncementWithOptions : Core.Model.Announcement
    {
        public int TimeOffset { get; set; }
        // TODO: Expiration time options?
    }
}
