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

    [Authorize]
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
    [Authorize]
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
            // todo: include error message in vm to indicate not saved.
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

        var extension = MimeTypes.GetMimeTypeExtensions(doc.ContentType).FirstOrDefault();
        if (!String.IsNullOrWhiteSpace(extension) && !doc.Name.EndsWith(extension))
        {
            doc.Name = doc.Name + extension;
        }
        System.Net.Mime.ContentDisposition cd = new System.Net.Mime.ContentDisposition
        {
            FileName = doc.Name,
            Inline = true
        };
        Response.Headers.Add("Content-Disposition", cd.ToString());
        return new FileStreamResult(stream, doc.ContentType);
    }

    [Authorize]
    public async Task<ActionResult> Update(
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
            var doc =
                await _documentService.GetSkinnyDocument(id);
            return View(_mapper.Map<DocumentWithOptions>(doc));
        }
        catch
        {
            // todo: add error message to vm.
            return RedirectToAction("Index", "Admin");
        }
    }


    [Authorize]
    [HttpPost]
    [ActionName("Update")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> PostUpdate(
        string clubInitials,
        DocumentWithOptions model,
        string returnUrl = null)
    {
        try
        {
            ViewData["ReturnUrl"] = returnUrl;

            model.CreatedBy = await GetUserStringAsync();
            model.CreatedDate = DateTime.UtcNow;
            model.CreatedLocalDate = DateTime.UtcNow.AddMinutes(0 - model.TimeOffset);
            if (!await _authService.CanUserEdit(User, clubInitials))
            {
                return Unauthorized();
            }
            await _documentService.UpdateDocument(model);

            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Admin");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            // todo: add error message to vm.
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
        var document = await _documentService.GetSkinnyDocument(id);
        return View(document);
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