using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Model;
using SailScores.Identity.Entities;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services;
using System;
using System.Threading.Tasks;
using Ganss.XSS;
using CoreServices = SailScores.Core.Services;

namespace SailScores.Web.Controllers
{
    public class AnnouncementController : Controller
    {


        private readonly CoreServices.IClubService _clubService;
        private readonly IAnnouncementService _announcementService;
        private readonly Services.IAuthorizationService _authService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly IHtmlSanitizer _sanitizer;

        public AnnouncementController(
                CoreServices.IClubService clubService,
                IAnnouncementService announcementService,
                Services.IAuthorizationService authService,
                UserManager<ApplicationUser> userManager,
                IHtmlSanitizer sanitizer,
                IMapper mapper)
        {
            _clubService = clubService;
            _announcementService = announcementService;
            _authService = authService;
            _userManager = userManager;
            _sanitizer = sanitizer;
            _mapper = mapper;
        }

        public async Task<ActionResult> Create(
            string clubInitials,
            Guid regattaId,
            string returnUrl = null)
        {

            ViewData["ReturnUrl"] = returnUrl;
            var vm = await _announcementService.GetBlankAnnouncementForRegatta(
                clubInitials,
                regattaId);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(
            string clubInitials,
            AnnouncementWithOptions model,
            string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            try
            {
                var clubId = await _clubService.GetClubId(clubInitials);
                if (!await _authService.CanUserEdit(User, clubId))
                {
                    return Unauthorized();
                }
                model.ClubId = clubId;
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                model.Content = _sanitizer.Sanitize(model.Content);
                model.CreatedBy = await GetUserStringAsync();
                model.CreatedDate = DateTime.UtcNow;
                model.CreatedLocalDate = DateTime.UtcNow.AddMinutes(0 - model.TimeOffset);
                await _announcementService.SaveNew(model);
                if (!string.IsNullOrWhiteSpace(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Admin");
            }
            catch
            {
                AnnouncementWithOptions vm;
                if (model.RegattaId.HasValue)
                {
                    vm = await _announcementService.GetBlankAnnouncementForRegatta(
                       clubInitials,
                       model.RegattaId.Value);
                
                } else
                {
                    vm = model;
                }
                return View(vm);
            }
        }

        [Authorize]
        public async Task<ActionResult> Edit(
            string clubInitials,
            Guid id,
            string returnUrl = null)
        {
            try
            {
                ViewData["ReturnUrl"] = returnUrl;
                if (!await _authService.CanUserEdit(User, clubInitials))
                {
                    return Unauthorized();
                }
                var announcement =
                    await _announcementService.GetAnnouncement(id);
                return View(announcement);
            }
            catch
            {
                return RedirectToAction("Index", "Admin");
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(
            string clubInitials,
            AnnouncementWithOptions model,
            string returnUrl = null)
        {
            try
            {
                if (!await _authService.CanUserEdit(User, clubInitials))
                {
                    return Unauthorized();
                }
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                model.Content = _sanitizer.Sanitize(model.Content);
                model.UpdatedBy = await GetUserStringAsync();
                model.UpdatedDate = DateTime.UtcNow;
                model.UpdatedLocalDate = DateTime.UtcNow.AddMinutes(0 - model.TimeOffset);
                await _announcementService.Update(model);

                if (!string.IsNullOrWhiteSpace(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Admin");
            }
            catch
            {
                return View(model);
            }
        }

        [Authorize]
        public async Task<ActionResult> Delete(
            string clubInitials,
            Guid id,
            string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!await _authService.CanUserEdit(User, clubInitials))
            {
                return Unauthorized();
            }
            var announcement = await _announcementService.GetAnnouncement(id);
            return View(announcement);
        }

        [Authorize]
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PostDelete(
            string clubInitials,
            Guid id,
            string returnUrl = null)
        {
            if (!await _authService.CanUserEdit(User, clubInitials))
            {
                return Unauthorized();
            }
            try
            {
                await _announcementService.Delete(id);

                if (!string.IsNullOrWhiteSpace(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Admin");
            }
            catch
            {
                var fleet = await _announcementService.GetAnnouncement(id);
                return View(fleet);
            }
        }

        private async Task<string> GetUserStringAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user.GetDisplayName();
        }
    }
}