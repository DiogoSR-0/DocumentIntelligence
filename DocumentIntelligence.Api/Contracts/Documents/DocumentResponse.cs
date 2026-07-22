using DocumentIntelligence.Api.Domain.Enums;

namespace DocumentIntelligence.Api.Contracts.Documents
{
    /// <summary>
    /// Representa os dados de um documento devolvidos pela API
    /// </summary>
    public sealed record DocumentResponse(
        Guid Id,
        string FileName,
        string ContentType,
        long SizeBytes,
        DocumentStatus Status,
        int? PageCount,
        DateTimeOffset CreatedAtUtc
    );
}
