using DocumentIntelligence.Api.Application.Abstractions.Documents;
using DocumentIntelligence.Api.Application.Abstractions.Storage;
using DocumentIntelligence.Api.Contracts.Documents.Extraction;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace DocumentIntelligence.Api.Infrastructure.Documents.Extraction
{
    /// <summary>
    /// Extrai texto de documentos PDF utilizando a biblioteca PdfPig.
    /// </summary>
    public sealed class PdfPigDocumentTextExtractor: IDocumentTextExtractor
    {
        /// <summary>
        /// Percorre todas as páginas do PDF e junta o texto
        /// respeitando, tanto quanto possível, a ordem visual do conteúdo.
        /// </summary>
        public Task<DocumentTextExtractionResult> ExtractAsync (Stream content, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(content);

            if(!content.CanRead)
            {
                throw new ArgumentException(
                    "O stream do documento não permite leitura.",
                    nameof(content));
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Garante que a leitura começa no início do ficheiro.
            if(content.CanSeek)
            {
                content.Position = 0;
            }

            using var pdfDocument = PdfDocument.Open(content);

            var extractedText = new StringBuilder();

            foreach (var page in pdfDocument.GetPages())
            {
                // Permite interromper a extração entre páginas.
                cancellationToken.ThrowIfCancellationRequested();

                // O valor true tenta preservar as mudanças de linha.
                var pageText = ContentOrderTextExtractor.GetText(
                    page, 
                    true);

                if (string.IsNullOrWhiteSpace(pageText))
                {
                    continue;
                }

                // Separa visualmente o texto de páginas diferentes.
                if (extractedText.Length > 0)
                {
                    extractedText.AppendLine();
                    extractedText.AppendLine();
                }

                extractedText.Append(pageText.Trim());
            }

            var result = new DocumentTextExtractionResult(
                extractedText.ToString(),
                pdfDocument.NumberOfPages);

            return Task.FromResult(result);
        }
    }
}
