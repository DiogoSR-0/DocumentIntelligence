using DocumentIntelligence.Api.Application.Abstractions.Storage;

namespace DocumentIntelligence.Api.Infrastructure.Storage
{
    /// <summary>
    /// Guarda os documentos no sistema de ficheiros local da aplicação.
    /// </summary>
    public sealed class LocalDocumentStorage(IWebHostEnvironment environment) : IDocumentStorage
    {
        private readonly string _storageRoot = Path.Combine(environment.ContentRootPath, "storage");

        public async Task<string> SaveAsync(
            Guid documentId,
            string originalFileName,
            Stream content,
            CancellationToken cancellationToken)
        {
            // Obtém apenas a extensão, sem confiar no caminho enviado pelo cliente.
            var extension = Path
                .GetExtension(Path.GetFileName(originalFileName))
                .ToLowerInvariant();

            // O Guid evita colisões entre ficheiros com o mesmo nome original.
            var storageKey = $"documents/{documentId:N}{extension}";

            var fullPath = GetFullPath(storageKey);

            var directory = Path.GetDirectoryName(fullPath)
                        ?? throw new InvalidOperationException(
                            "Não foi possível determinar a pasta do documento.");

            Directory.CreateDirectory(directory);

            try
            {
                await using var destination = new FileStream(
                    fullPath,
                    new FileStreamOptions
                    {
                        Mode = FileMode.CreateNew,
                        Access = FileAccess.Write,
                        Share = FileShare.None,
                        Options = FileOptions.Asynchronous,
                        BufferSize = 81920
                    });

                await content.CopyToAsync(
                    destination,
                    cancellationToken);

                return storageKey;
            }
            catch
            {
                // Remove um ficheiro parcial caso a cópia seja interrompida.
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }

                throw;
            }
        }

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fullPath = GetFullPath(storageKey);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.CompletedTask;
        }

        private string GetFullPath(string storageKey)
        {
            var relativePath = storageKey.Replace(
                '/',
                Path.DirectorySeparatorChar);

            var fullPath = Path.GetFullPath(
                Path.Combine(_storageRoot, relativePath));

            var normalizedRoot =
                Path.GetFullPath(_storageRoot) +
                Path.DirectorySeparatorChar;

            // Impede que uma chave maliciosa aceda a ficheiros fora da pasta storage.
            if (!fullPath.StartsWith(
                    normalizedRoot,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "A storage key indicada é inválida.");
            }

            return fullPath;
        }
    }

}
