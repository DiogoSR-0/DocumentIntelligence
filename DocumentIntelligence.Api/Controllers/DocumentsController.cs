using DocumentIntelligence.Api.Contracts.Documents;
using DocumentIntelligence.Api.Domain.Entities;
using DocumentIntelligence.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DocumentIntelligence.Api.Application.Abstractions.Storage;
using DocumentIntelligence.Api.Application.Abstractions.Documents;
using DocumentIntelligence.Api.Domain.Enums;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DocumentIntelligence.Api.Controllers
{
    /// <summary>
    /// Disponibiliza operações com documentos.
    /// </summary>
    [ApiController]
    [Route("api/documents")]
    public sealed class DocumentsController(
        DocumentIntelligenceDbContext dbContext, 
        IDocumentStorage documentStorage,
        IDocumentTextExtractor documentTextExtractor,
        ILogger<DocumentsController> logger) : ControllerBase
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
                    document.Status,
                    document.PageCount,
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
                    document.Status,
                    document.PageCount,
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
                // Guarda primeiro o ficheiro no armazenamento local.
                await using (var storageContent = file.OpenReadStream())
                {
                    storageKey = await documentStorage.SaveAsync(
                        document.Id,
                        originalFileName,
                        storageContent,
                        cancellationToken);
                }

                document.StorageKey = storageKey;
                document.Status = DocumentStatus.Processing;

                try
                {
                    // Abre um novo stream para a extração de texto.
                    await using var extractionContent = file.OpenReadStream();

                    var extractionResult = await documentTextExtractor.ExtractAsync(extractionContent, cancellationToken);

                    document.ExtractedText = extractionResult.Text;
                    document.PageCount = extractionResult.PageCount;
                    document.ProcessingError = null;
                    document.Status = DocumentStatus.Completed;

                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Um cancelamento do pedido não é tratado como erro do PDF.
                    throw;
                }
                catch (Exception exception) 
                {
                    // O upload foi concluído, mas não foi possível extrair o texto
                    document.ExtractedText = null;
                    document.PageCount = null;
                    document.ProcessingError = "Náo foi possível extrair o texto do documento.";
                    document.Status = DocumentStatus.Failed;

                    logger.LogWarning(exception, "Não foi possível extrair o texto do documento {DocumentId}.", document.Id);
                }

                dbContext.Documents.Add(document);

                // Guarda no PostgreSQL os metadados e o resultado da extração.
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                // Se o registo na base de dados falhar, remove o ficheiro
                // para evitar ficheiros órfãos no armazenamento.
                if (storageKey is not null)
                {
                    try
                    {
                        await documentStorage.DeleteAsync(storageKey, CancellationToken.None);
                    }
                    catch (Exception cleanupException)
                    {
                        logger.LogError(cleanupException, "Não foi possível remover o ficheiro {StorageKey} depois de uma falha no upload.", storageKey);
                    }
                }

                throw;
            }

            var response = new DocumentResponse(
                document.Id,
                document.FileName,
                document.ContentType,
                document.SizeBytes,
                document.Status,
                document.PageCount,
                document.CreatedAtUtc);

            return CreatedAtRoute(
                nameof(GetByIdAsync),
                new { id = document.Id },
                response);
        }

        /// <summary>
        /// Descarrega o ficheiro associado a um documento.
        /// </summary>
        [HttpGet("{id:guid}/file")]
        [Produces("application/pdf")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadAsync(Guid id, CancellationToken cancellationToken)
        {
            var document = await dbContext.Documents
                .AsNoTracking()
                .Where(document => document.Id == id)
                .Select(document => new
                {
                    document.FileName,
                    document.ContentType,
                    document.StorageKey
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (document == null)
            {
                return NotFound();
            }

            // Os documentos antigos foram criados apenas com metadados
            // e não têm um ficheiro físico associado.
            if (string.IsNullOrWhiteSpace(document.StorageKey))
            {
                return Problem(
                    title: "Ficheiro não disponível.",
                    detail: "Este documento não possui um ficheiro armazenado",
                    statusCode: StatusCodes.Status404NotFound);
            }

            var content = await documentStorage.OpenReadAsync(document.StorageKey, cancellationToken);

            if (content == null)
            {
                return Problem(
                    title: "Ficheiro não foi encontrado.",
                    detail: "O registo existe, mas o ficheiro não foi encontrado no armazenamento",
                    statusCode: StatusCodes.Status404NotFound);
            }

            return File(content, document.ContentType, document.FileName, enableRangeProcessing: true);

        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            var document = await dbContext.Documents
                .SingleOrDefaultAsync(
                document => document.Id == id,
                cancellationToken);

            if (document == null)
            {
                return NotFound();
            }

            // Remove primeiro o registo da base de dados
            dbContext.Documents .Remove(document);

            await dbContext.SaveChangesAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(document.StorageKey))
            {
                try
                {
                    // Usa CancellationToken.None porque o registo já foi removido.
                    // Mesmo que o cliente cancele o pedido, tentamos concluir a limpeza.
                    await documentStorage.DeleteAsync(document.StorageKey, CancellationToken.None);
                }
                catch (Exception exception)
                {
                    // A eliminação principal já aconteceu.
                    // Registamos a falha para permitir limpar o ficheiro posteriormente.
                    logger.LogError(
                        exception,
                        "O documento {DocumentId} foi removido da base de dados, " +
                        "mas não foi possível eliminar o ficheiro {StorageKey}.",
                        document.Id,
                        document.StorageKey);
                }
            }

            return NoContent();
        }

        [HttpGet("{id:guid}/text")]
        [ProducesResponseType(typeof(DocumentTextResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DocumentTextResponse>> GetTextAsync(Guid id, CancellationToken cancellationToken)
        {
            var response = await dbContext.Documents
                .AsNoTracking()
                .Where(document => document.Id == id)
                .Select(document => new DocumentTextResponse(
                    document.Id,
                    document.Status,
                    document.PageCount,
                    document.ExtractedText,
                    document.ProcessingError))
                .SingleOrDefaultAsync(cancellationToken);

            if(response == null)
            {
                return NotFound();
            }

            return Ok(response);
        }
    }
}
