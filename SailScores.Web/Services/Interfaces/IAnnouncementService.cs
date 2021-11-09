using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Services.Interfaces;

public interface IAnnouncementService
{
    Task<AnnouncementWithOptions> GetBlankAnnouncementForRegatta(string clubInitials, Guid regattaId);
    Task<IEnumerable<Announcement>> GetRegattaAnnouncements(Guid regattaId);
    Task SaveNew(Announcement model);
    Task<Announcement> GetAnnouncement(Guid id);
    Task Delete(Guid id);
    Task Update(Announcement model);
}