using System.ComponentModel.DataAnnotations;

namespace DocumentIntelligence.Api.Contracts.Documents
{
    /// <summary>
    /// Representa o ficheiro enviado para registo na aplicação.
    /// </summary>
    public sealed class UploadDocumentRequest
    {
        [Required]
        public IFormFile File { get; set; } = null!;
    }
}
