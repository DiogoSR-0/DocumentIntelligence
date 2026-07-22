namespace DocumentIntelligence.Api.Contracts.Documents.Extraction
{
    /// <summary>
    /// Representa o resultado da extração de texto de um documento.
    /// </summary>
    public sealed record DocumentTextExtractionResult(string Text, int PageCount)
    {
    }
}
