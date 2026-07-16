using DocumentIntelligence.Api.Contracts.Documents;
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
    }
}
