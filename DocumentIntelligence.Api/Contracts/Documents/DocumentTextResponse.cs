using DocumentIntelligence.Api.Domain.Enums;

namespace DocumentIntelligence.Api.Contracts.Documents
{
    /// <summary>
    /// Representa o resultado do processamento textual de um documento.
    /// </summary>
    public sealed record DocumentTextResponse(
        Guid Id,
        DocumentStatus status,
        int? PageCount,
        string? Text,
        string? ProcessingError
        );
}
