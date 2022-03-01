using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SailScores.Identity.Entities;
using SailScores.Web.Models.SailScores;
using Ganss.XSS;
using SailScores.Web.Services.Interfaces;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;
using System.IO;

namespace SailScores.Web.Controllers;

public class DocumentController : Controller
{


    private readonly CoreServices.IClubService _clubService;
    private readonly IDocumentService _documentService;
    private readonly IAuthorizationService _authService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;
    private readonly IHtmlSanitizer _sanitizer;

    public DocumentController(
        CoreServices.IClubService clubService,
        IDocumentService regattaDocumentService,
        IAuthorizationService authService,
        UserManager<ApplicationUser> userManager,
        IHtmlSanitizer sanitizer,
        IMapper mapper)
    {
        _clubService = clubService;
        _documentService = regattaDocumentService;
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
        var vm = await _documentService.GetDocumentUploadForRegatta(
            clubInitials,
            regattaId);

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(
        string clubInitials,
        DocumentWithOptions model,
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

            model.CreatedBy = await GetUserStringAsync();
            model.CreatedDate = DateTime.UtcNow;
            model.CreatedLocalDate = DateTime.UtcNow.AddMinutes(0 - model.TimeOffset);
            await _documentService.SaveNew(model);
            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Admin");
        }
        catch
        {
            DocumentWithOptions vm;
            if (model.RegattaId.HasValue)
            {
                vm = await _documentService.GetDocumentUploadForRegatta(
                    clubInitials,
                    model.RegattaId.Value);
                
            } else
            {
                vm = model;
            }
            return View(vm);
        }
    }

    public async Task<FileStreamResult> GetDocument(Guid id)
    {
        var doc = await _documentService.GetDocument(id);

        var stream = new MemoryStream();
        stream.Write(doc.FileContents, 0, doc.FileContents.Length);
        stream.Position = 0;
        return new FileStreamResult(stream, doc.ContentType);
    }

    // Replace
    //[Authorize]
    //public async Task<ActionResult> Edit(
    //    string clubInitials,
    //    Guid id,
    //    string returnUrl = null)
    //{
    //    try
    //    {
    //        ViewData["ReturnUrl"] = returnUrl;
    //        if (!await _authService.CanUserEdit(User, clubInitials))
    //        {
    //            return Unauthorized();
    //        }
    //        var announcement =
    //            await _regattaDocumentService.GetDocument(id);
    //        return View(announcement);
    //    }
    //    catch
    //    {
    //        return RedirectToAction("Index", "Admin");
    //    }
    //}


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
        var announcement = await _documentService.GetDocument(id);
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
            await _documentService.Delete(id);

            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Admin");
        }
        catch
        {
            var fleet = await _documentService.GetDocument(id);
            return View(fleet);
        }
    }

    private async Task<string> GetUserStringAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user.GetDisplayName();
    }
}