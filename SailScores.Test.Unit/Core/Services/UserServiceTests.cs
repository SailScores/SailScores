using Microsoft.Extensions.Caching.Memory;
using SailScores.Core.Services;
using SailScores.Database;
using SailScores.Database.Entities;
using SailScores.Test.Unit.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Unit.Core.Services
{
    public class UserServiceTests
    {
        private readonly UserService _service;
        private readonly ISailScoresContext _context;
        private readonly Guid _clubId;
        private readonly string _testUserEmail = "test@example.com";
        private readonly string _adminUserEmail = "admin@example.com";
        private readonly MemoryCache _cache;

        public UserServiceTests()
        {
            _context = InMemoryContextBuilder.GetContext();
            _clubId = _context.Clubs.First().Id;
            _cache = new MemoryCache(new MemoryCacheOptions());
            
            _service = new UserService(_context, _cache);
            
            // Add test permissions
            _context.UserPermissions.Add(new UserClubPermission
            {
                Id = Guid.NewGuid(),
                UserEmail = _testUserEmail,
                ClubId = _clubId,
                CanEditAllClubs = false,
                PermissionLevel = PermissionLevel.RaceScorekeeper,
                Created = DateTime.UtcNow,
                CreatedBy = "Test"
            });
            
            _context.UserPermissions.Add(new UserClubPermission
            {
                Id = Guid.NewGuid(),
                UserEmail = _adminUserEmail,
                ClubId = null,
                CanEditAllClubs = true,
                PermissionLevel = PermissionLevel.ClubAdministrator,
                Created = DateTime.UtcNow,
                CreatedBy = "Test"
            });
            
            _context.SaveChanges();
        }

        [Fact]
        public async Task CanEditRaces_RaceScorekeeper_ReturnsTrue()
        {
            var result = await _service.CanEditRaces(_testUserEmail, _clubId);
            Assert.True(result);
        }

        [Fact]
        public async Task CanEditSeries_RaceScorekeeper_ReturnsFalse()
        {
            var result = await _service.CanEditSeries(_testUserEmail, _clubId);
            Assert.False(result);
        }

        [Fact]
        public async Task CanEditRaces_FullAdmin_ReturnsTrue()
        {
            var result = await _service.CanEditRaces(_adminUserEmail, _clubId);
            Assert.True(result);
        }

        [Fact]
        public async Task CanEditSeries_FullAdmin_ReturnsTrue()
        {
            var result = await _service.CanEditSeries(_adminUserEmail, _clubId);
            Assert.True(result);
        }

        [Fact]
        public async Task GetPermissionLevel_ExistingUser_ReturnsCorrectLevel()
        {
            var result = await _service.GetPermissionLevel(_testUserEmail, _clubId);
            Assert.Equal(PermissionLevel.RaceScorekeeper, result);
        }

        [Fact]
        public async Task GetPermissionLevel_NonExistingUser_ReturnsNull()
        {
            var result = await _service.GetPermissionLevel("nonexistent@example.com", _clubId);
            Assert.Null(result);
        }

        [Fact]
        public async Task AddPermission_WithPermissionLevel_AddsCorrectLevel()
        {
            var newUserEmail = "newuser@example.com";
            await _service.AddPermission(_clubId, newUserEmail, "Test", PermissionLevel.SeriesScorekeeper);
            
            var permission = await _service.GetPermissionLevel(newUserEmail, _clubId);
            Assert.Equal(PermissionLevel.SeriesScorekeeper, permission);
        }

        [Fact]
        public async Task CanEditSeries_SeriesScorekeeper_ReturnsTrue()
        {
            var seriesUserEmail = "series@example.com";
            await _service.AddPermission(_clubId, seriesUserEmail, "Test", PermissionLevel.SeriesScorekeeper);
            
            var result = await _service.CanEditSeries(seriesUserEmail, _clubId);
            Assert.True(result);
        }

        [Fact]
        public async Task CanEditSeries_ClubAdministrator_ReturnsTrue()
        {
            var clubAdminEmail = "clubadmin@example.com";
            await _service.AddPermission(_clubId, clubAdminEmail, "Test", PermissionLevel.ClubAdministrator);
            
            var result = await _service.CanEditSeries(clubAdminEmail, _clubId);
            Assert.True(result);
        }
    }
}
