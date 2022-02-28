using SailScores.Core.Model;
using System;
using System.Threading.Tasks;

namespace SailScores.Core.Services;

public interface IDocumentService
{
    Task Save(Document file);
    Task<Document> GetDocument(Guid id);
}
