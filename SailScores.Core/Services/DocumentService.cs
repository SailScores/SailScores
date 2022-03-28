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

    public async Task DeleteDocument(Guid id)
    {
        var doc = new Db.Document
        {
            Id = id
        };
        _dbContext.Documents.Remove(doc);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<Document> GetDocument(Guid id)
    {
        var doc = await _dbContext.Documents.SingleAsync(d => d.Id == id);

        return _mapper.Map<Document>(doc);
    }

    public async Task<Document> GetSkinnyDocument(Guid id)
    {

        var dbDoc = await _dbContext.Documents.Where(
            d => d.Id == id)
            .Select(d => new Document
            {
                Id = d.Id,
                RegattaId = d.RegattaId,
                ClubId = d.ClubId,
                Name = d.Name,
                CreatedDate = d.CreatedDate,
                CreatedLocalDate = d.CreatedLocalDate,
                CreatedBy = d.CreatedBy
            }).SingleAsync();
        return _mapper.Map<Document>(dbDoc);
    }

    public async Task Save(Document file)
    {
        if (file.Id == default)
        {
            _dbContext.Documents.Add(_mapper.Map<Db.Document>(file));
        } else
        {
            var dbDoc = await _dbContext.Documents
                .SingleOrDefaultAsync(
                d => d.Id == file.Id);
            dbDoc.CreatedDate = file.CreatedDate;
            dbDoc.CreatedLocalDate = file.CreatedLocalDate;
            dbDoc.CreatedBy = file.CreatedBy;
            dbDoc.Name = file.Name;
            if(file.FileContents != null)
            {
                dbDoc.FileContents = file.FileContents;
                dbDoc.ContentType = file.ContentType;
            }
        }

        await _dbContext.SaveChangesAsync();
    }
}
