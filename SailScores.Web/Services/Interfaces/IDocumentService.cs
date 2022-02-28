using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Services.Interfaces;

public interface IDocumentService
{
    Task<DocumentWithOptions> GetDocumentUploadForRegatta(string clubInitials, Guid regattaId);
    Task<IEnumerable<Document>> GetRegattaDocuments(Guid regattaId);
    Task SaveNew(DocumentWithOptions model);
    Task<Document> GetDocument(Guid id);
    Task Delete(Guid id);
}