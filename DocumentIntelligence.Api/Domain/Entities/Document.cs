using DocumentIntelligence.Api.Domain.Enums;

namespace DocumentIntelligence.Api.Domain.Entities
{
    public class Document
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set;  } = string.Empty;
        public long SizeBytes { get; set; }

        /// <summary>
        /// Identificador utilizado para localizar o ficheiro no armazenamento.
        /// </summary>
        public string? StorageKey { get; set; }

        /// <summary>
        /// Estado atual do processamento do documento.
        /// </summary>
        public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;

        /// <summary>
        /// Número de páginas encontrado durante a extração.
        /// É null enquanto o documento ainda não foi processado.
        /// </summary>
        public int? PageCount { get; set; }

        /// <summary>
        /// Texto extraído do documento.
        /// É null enquanto a extração ainda não tiver sido concluída.
        /// </summary>
        public string? ExtractedText { get; set; }

        /// <summary>
        /// Descrição do erro ocorrido durante o processamento.
        /// É preenchida apenas quando o estado é Failed.
        /// </summary>
        public string? ProcessingError { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    }
}
