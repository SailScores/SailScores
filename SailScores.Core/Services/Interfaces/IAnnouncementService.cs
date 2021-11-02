using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Core.Model;
using Entities = SailScores.Database.Entities;

namespace SailScores.Core.Services
{
    public interface IAnnouncementService
    {
        Task Delete(Guid announcementId);
        Task<Announcement> Get(Guid announcementId);
        Task SaveNew(Announcement announcement);
        Task Update(Announcement announcement);
    }
}