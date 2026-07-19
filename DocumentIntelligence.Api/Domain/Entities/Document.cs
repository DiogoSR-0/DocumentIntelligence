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
        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    }
}
