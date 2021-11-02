using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public class AnnouncementService : IAnnouncementService
    {
        private Core.Services.IAnnouncementService _coreAnnouncementService;

        public AnnouncementService(
            Core.Services.IAnnouncementService announcementService
            )
        {
            _coreAnnouncementService = announcementService;
        }

        public async Task Delete(Guid announcementId)
        {
            await _coreAnnouncementService.Delete(announcementId);
        }

        public async Task<Announcement> GetAnnouncement(Guid announcementId)
        {
            return await _coreAnnouncementService.Get(announcementId);
        }

        public async Task<AnnouncementWithOptions> GetBlankAnnouncementForRegatta(string clubInitials, Guid regattaId)
        {
            return new AnnouncementWithOptions
            {
                RegattaId = regattaId
            };
        }

        public Task<IEnumerable<Announcement>> GetRegattaAnnouncements(Guid regattaId)
        {
            throw new NotImplementedException();
        }

        public async Task SaveNew(Announcement announcement)
        {

            await _coreAnnouncementService.SaveNew(announcement);
        }

        public async Task Update(Announcement announcement)
        {
            await _coreAnnouncementService.Update(announcement);
        }
    }
}
