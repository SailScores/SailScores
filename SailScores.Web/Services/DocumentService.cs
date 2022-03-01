using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services.Interfaces;
using System.IO;

namespace SailScores.Web.Services;

public class DocumentService : Interfaces.IDocumentService
{
    private readonly Core.Services.IDocumentService _coreService;
    private readonly IMapper _mapper;

    public DocumentService(
        Core.Services.IDocumentService coreService,
        IMapper mapper)
    {
        _coreService = coreService;
        _mapper = mapper;
    }

    public Task Delete(Guid id)
    {
        return _coreService.DeleteDocument(id);
    }

    public Task<Document> GetDocument(Guid id)
    {
        return _coreService.GetDocument(id);
    }

    public async Task<DocumentWithOptions> GetDocumentUploadForRegatta(string clubInitials, Guid regattaId)
    {
        return new DocumentWithOptions();
    }

    public Task<IEnumerable<Document>> GetRegattaDocuments(Guid regattaId)
    {
        throw new NotImplementedException();
    }

    public async Task SaveNew(DocumentWithOptions model)
    {

        using (var memoryStream = new MemoryStream())
        {
            await model.File.CopyToAsync(memoryStream);

            // Upload the file if less than 2 MB
            if (memoryStream.Length < 2097152)
            {
                model.FileContents = memoryStream.ToArray();
                model.ContentType = model.File.ContentType;

                await _coreService.Save(model);
            }
            else
            {
                //ModelState.AddModelError("File", "The file is too large.");
            }
        }
    }

}