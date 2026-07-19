using DocumentIntelligence.Api.Contracts.Documents;
using DocumentIntelligence.Api.Domain.Entities;
using DocumentIntelligence.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DocumentIntelligence.Api.Application.Abstractions.Storage;

namespace DocumentIntelligence.Api.Controllers
{
    /// <summary>
    /// Disponibiliza operações com documentos.
    /// </summary>
    [ApiController]
    [Route("api/documents")]
    public sealed class DocumentsController(DocumentIntelligenceDbContext dbContext, IDocumentStorage documentStorage) : ControllerBase
    {
        // Permite até um maximo de 10MB o tamanho do ficheiro
        private const long MaxFileSizeBytes = 10 * 1024 * 1024;

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

        /// <summary>
        /// Recebe um ficheiro PDF, guarda-o no armazenamento
        /// e regista os seus metadados na base de dados.
        /// </summary>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
        public async Task<ActionResult<DocumentResponse>> CreateAsync(
            [FromForm] UploadDocumentRequest request,
            CancellationToken cancellationToken)
        {
            var file = request.File;

            if (file.Length == 0)
            {
                return BadRequest("O ficheiro enviado está vazio.");
            }

            if (file.Length > MaxFileSizeBytes)
            {
                return Problem(
                    title: "Ficheiro demasiado grande.",
                    detail: "O tamanho máximo permitido é 10 MB.",
                    statusCode: StatusCodes.Status413PayloadTooLarge);
            }

            // Remove qualquer caminho que possa ter sido enviado no nome do ficheiro.
            var originalFileName = Path.GetFileName(file.FileName);

            var extension = Path.GetExtension(originalFileName);

            var isPdfExtension = string.Equals(
                extension,
                ".pdf",
                StringComparison.OrdinalIgnoreCase);

            var isPdfContentType = string.Equals(
                file.ContentType,
                "application/pdf",
                StringComparison.OrdinalIgnoreCase);

            if (!isPdfExtension || !isPdfContentType)
            {
                return BadRequest(
                    "Apenas ficheiros PDF são permitidos.");
            }

            var document = new Document
            {
                FileName = originalFileName,
                ContentType = file.ContentType,
                SizeBytes = file.Length
            };

            string? storageKey = null;

            try
            {
                await using var content = file.OpenReadStream();

                // Guarda o ficheiro físico antes de registar os metadados.
                storageKey = await documentStorage.SaveAsync(
                    document.Id,
                    originalFileName,
                    content,
                    cancellationToken);

                document.StorageKey = storageKey;

                dbContext.Documents.Add(document);

                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                // Se o registo na base de dados falhar, remove o ficheiro
                // para evitar ficheiros órfãos no armazenamento.
                if (storageKey is not null)
                {
                    await documentStorage.DeleteAsync(
                        storageKey,
                        CancellationToken.None);
                }

                throw;
            }

            var response = new DocumentResponse(
                document.Id,
                document.FileName,
                document.ContentType,
                document.SizeBytes,
                document.CreatedAtUtc);

            return CreatedAtRoute(
                nameof(GetByIdAsync),
                new { id = document.Id },
                response);
        }

    }
}
