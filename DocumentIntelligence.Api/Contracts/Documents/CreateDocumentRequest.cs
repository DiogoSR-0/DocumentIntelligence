using System.ComponentModel.DataAnnotations;

namespace DocumentIntelligence.Api.Contracts.Documents
{
    /// <summary>
    /// Representa os dados necessários para registar um documento.
    /// </summary>
    public sealed class CreateDocumentRequest
    {
        // FileName é obrigatório e tem no máximo 500 caracteres
        [Required]
        [StringLength(500)]
        public string FileName { get; init; } = string.Empty;

        // ContentType é obrigatório e tem no máximo 200 caracteres
        [Required]
        [StringLength(200)]
        public string ContentType { get; init; } = string.Empty;


        // SizeByte tem de ser maior do que zero
        [Range(1, long.MaxValue)]
        public long SizeBytes {  get; init; }
    }
}
