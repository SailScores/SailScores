using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AutoMapper.QueryableExtensions;
using System.Linq;
using SailScores.Core.Model;
using Db = SailScores.Database.Entities;
using SailScores.Api.Dtos;

namespace SailScores.Core.Services
{
    public class ClubRequestService : IClubRequestService
    {
        private readonly ISailScoresContext _dbContext;
        private readonly IMapper _mapper;

        public ClubRequestService(
            ISailScoresContext dbContext,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<IList<ClubRequest>> GetPendingRequests()
        {
            var dbObj = await _dbContext.ClubRequests
                .Where(c => c.RequestApproved == null
                || c.TestClubId == null
                || c.VisibleClubId == null)
                .OrderBy(c => c.RequestSubmitted)
                .ToListAsync();

            return _mapper.Map<IList<ClubRequest>>(dbObj);
        }

        public async Task<ClubRequest> GetRequest(Guid id)
        {
            var dbObj = await _dbContext.ClubRequests
                .Where(c => c.Id == id)
                .FirstAsync();

            return _mapper.Map<ClubRequest>(dbObj);
        }

        public async Task Submit(ClubRequest request)
        {
            var dbObj = _mapper.Map<Db.ClubRequest>(request);
            _dbContext.ClubRequests.Add(dbObj);

            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateRequest(ClubRequest request)
        {
            var existingEntry = await _dbContext.ClubRequests.SingleAsync(r => r.Id == request.Id);

            existingEntry.ClubName = request.ClubName;
            existingEntry.ClubInitials = request.ClubInitials;
            existingEntry.ClubLocation = request.ClubLocation;
            existingEntry.ClubWebsite = request.ClubWebsite;
            existingEntry.ContactName = request.ContactName;
            existingEntry.ContactEmail = request.ContactEmail;
            existingEntry.Classes = request.Classes;
            existingEntry.TypicalDiscardRules = request.TypicalDiscardRules;
            existingEntry.Comments = request.Comments;
            existingEntry.AdminNotes = request.AdminNotes;
            existingEntry.TestClubId = request.TestClubId;
            existingEntry.VisibleClubId = request.VisibleClubId;
        }
    }
}
