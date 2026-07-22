using DocumentIntelligence.Api.Contracts.Documents.Extraction;

namespace DocumentIntelligence.Api.Application.Abstractions.Documents
{
    /// <summary>
    /// Define a operação necessária para extrair texto de um documento.
    /// </summary>
    public interface IDocumentTextExtractor
    {
        /// <summary>
        /// Extrai o texto existente no conteúdo do documento.
        /// </summary>
        Task<DocumentTextExtractionResult> ExtractAsync(Stream content, CancellationToken cancellationToken);
    }
}
