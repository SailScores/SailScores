using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Core.Model;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Db = SailScores.Database.Entities;

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
                .Where(c => !(c.Complete ?? false))
                .Where(c => c.RequestApproved == null
                || c.TestClubId == null
                || c.VisibleClubId == null)
                .OrderBy(c => c.RequestSubmitted)
                .ToListAsync().ConfigureAwait(false);

            return _mapper.Map<IList<ClubRequest>>(dbObj);
        }

        public async Task<ClubRequest> GetRequest(Guid id)
        {
            var dbObj = await _dbContext.ClubRequests
                .Where(c => c.Id == id)
                .FirstAsync().ConfigureAwait(false);

            return _mapper.Map<ClubRequest>(dbObj);
        }

        public async Task Submit(ClubRequest clubRequest)
        {
            var dbObj = _mapper.Map<Db.ClubRequest>(clubRequest);
            _dbContext.ClubRequests.Add(dbObj);

            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task UpdateRequest(ClubRequest clubRequest)
        {
            if(clubRequest == null)
            {
                throw new ArgumentNullException(nameof(clubRequest));
            }
            var existingEntry = await _dbContext.ClubRequests
                .SingleAsync(r => r.Id == clubRequest.Id).ConfigureAwait(false);

            existingEntry.ClubName = clubRequest.ClubName;
            existingEntry.ClubInitials = clubRequest.ClubInitials;
            existingEntry.ClubLocation = clubRequest.ClubLocation;
            existingEntry.ClubWebsite = clubRequest.ClubWebsite;
            existingEntry.ContactName = clubRequest.ContactName;
            existingEntry.ContactEmail = clubRequest.ContactEmail;
            existingEntry.Classes = clubRequest.Classes;
            existingEntry.TypicalDiscardRules = clubRequest.TypicalDiscardRules;
            existingEntry.Comments = clubRequest.Comments;
            existingEntry.AdminNotes = clubRequest.AdminNotes;
            existingEntry.TestClubId = clubRequest.TestClubId;
            existingEntry.VisibleClubId = clubRequest.VisibleClubId;
            if (existingEntry.RequestApproved == null)
            {
                existingEntry.RequestApproved = clubRequest.RequestApproved;
            }
        }
    }
}
