using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using SailScores.Core.Model;
using Db = SailScores.Database.Entities;

namespace SailScores.Core.Services
{
    public class AnnouncementService : IAnnouncementService
    {
        private readonly ISailScoresContext _dbContext;
        private readonly IMapper _mapper;


        public AnnouncementService(
            ISailScoresContext dbContext,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task Delete(Guid announcementId)
        {
            var dbEntity = await _dbContext.Announcements.SingleAsync(a => a.Id == announcementId);
            dbEntity.IsDeleted = true;

            _dbContext.SaveChanges();
        }

        public async Task<Announcement> Get(Guid announcementId)
        {
            var dbEntity = await _dbContext.Announcements.SingleAsync(a => a.Id == announcementId);
            return _mapper.Map<Announcement>(dbEntity);
        }

        public async Task SaveNew(Announcement announcement)
        {
            var dbEntity = _mapper.Map<Db.Announcement>(announcement);
            _dbContext.Announcements.Add(dbEntity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Update(Announcement announcement)
        {
            var currentDbEntity = await _dbContext.Announcements.SingleAsync(a => a.Id == announcement.Id);
            currentDbEntity.Content = announcement.Content;
            currentDbEntity.UpdatedLocalDate = announcement.UpdatedLocalDate;
            currentDbEntity.UpdatedBy = announcement.UpdatedBy;
            currentDbEntity.UpdatedDate = announcement.UpdatedDate;
            await _dbContext.SaveChangesAsync();

        }
    }
}
