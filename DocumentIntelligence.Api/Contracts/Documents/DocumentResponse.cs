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
        DateTimeOffset CreatedAtUtc
    );
}
