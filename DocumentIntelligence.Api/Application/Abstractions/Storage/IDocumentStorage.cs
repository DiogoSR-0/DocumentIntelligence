namespace DocumentIntelligence.Api.Application.Abstractions.Storage
{
    /// <summary>
    /// Define as operações necessárias para armazenar ficheiros de documentos.
    /// </summary>
    public interface IDocumentStorage
    {
        /// <summary>
        /// Guarda o conteúdo de um documento e devolve a chave
        /// utilizada para o localizar posteriormente.
        /// </summary>
        Task<string> SaveAsync(
            Guid documentId,
            string originalFileName,
            Stream content,
            CancellationToken cancellationToken);

        /// <summary>
        /// Remove um ficheiro anteriormente armazenado.
        /// </summary>
        Task DeleteAsync(string storageKey, CancellationToken cancellationToken);

        /// <summary>
        /// Abre um ficheiro armazenado para leitura.
        /// Devolve null caso o ficheiro já não exista.
        /// </summary>
        Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
    }
}
