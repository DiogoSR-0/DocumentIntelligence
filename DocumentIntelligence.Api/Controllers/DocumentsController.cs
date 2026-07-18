using DocumentIntelligence.Api.Contracts.Documents;
using DocumentIntelligence.Api.Domain.Entities;
using DocumentIntelligence.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentIntelligence.Api.Controllers
{
    /// <summary>
    /// Disponibiliza operações com documentos.
    /// </summary>
    [ApiController]
    [Route("api/documents")]
    public sealed class DocumentsController(DocumentIntelligenceDbContext dbContext) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<DocumentResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<DocumentResponse>>> GetAllAsync(CancellationToken cancellationToken)
        {
            // AsNoTracking melhora consultas em que os dados não serão alterados.
            var documents = await dbContext.Documents
                .AsNoTracking()
                .OrderByDescending(document => document.CreatedAtUtc)
                .Select(document => new DocumentResponse(
                    document.Id,
                    document.FileName,
                    document.ContentType,
                    document.SizeBytes,
                    document.CreatedAtUtc))
                .ToListAsync(cancellationToken);

            return Ok(documents);
        }

        /// <summary>
        /// Obtém um documento através do seu identificador.
        /// </summary>
        [HttpGet("{id:guid}", Name = nameof(GetByIdAsync))]
        [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DocumentResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var document = await dbContext.Documents
                .AsNoTracking()
                .Where(document => document.Id == id)
                .Select(document => new DocumentResponse(
                    document.Id,
                    document.FileName,
                    document.ContentType,
                    document.SizeBytes,
                    document.CreatedAtUtc))
                .SingleOrDefaultAsync(cancellationToken);

            if (document == null)
            {
                return NotFound();
            }

            return Ok(document);
        }

        [HttpPost]
        [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DocumentResponse>> CreateAsync(
            [FromBody] CreateDocumentRequest request, 
            CancellationToken cancellationToken) 
        {
            var document = new Document
            {
                FileName = request.FileName.Trim(),
                ContentType = request.ContentType.Trim(),
                SizeBytes = request.SizeBytes,
            };

            // Coloca a nova entidade sob acompanhamento do DbContext
            dbContext.Documents.Add(document);

            // Executa o INSERT no PostgreSQL
            await dbContext.SaveChangesAsync(cancellationToken);

            var response = new DocumentResponse(
                document.Id,
                document.FileName,
                document.ContentType,
                document.SizeBytes,
                document.CreatedAtUtc);

            return CreatedAtRoute(nameof(GetByIdAsync), new { id = document.Id }, response);
        } 

    }
}
