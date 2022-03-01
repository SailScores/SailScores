using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using SailScores.Core.Model;
using Db = SailScores.Database.Entities;

namespace SailScores.Core.Services;

public class DocumentService : IDocumentService
{
    private readonly ISailScoresContext _dbContext;
    private readonly IMapper _mapper;

    public DocumentService(
        ISailScoresContext dbContext,
        IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public Task DeleteDocument(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<Document> GetDocument(Guid id)
    {
        var doc = await _dbContext.Documents.SingleAsync(d => d.Id == id);

        return _mapper.Map<Document>(doc);
    }

    public async Task Save(Document file)
    {
        _dbContext.Documents.Add(_mapper.Map<Db.Document>(file));

        await _dbContext.SaveChangesAsync();
    }
}
