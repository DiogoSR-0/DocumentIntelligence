namespace DocumentIntelligence.Api.Domain.Enums
{
    /// <summary>
    /// Representa o estado atual do processamento de um documento.
    /// </summary>
    public enum DocumentStatus
    {
        /// <summary>
        /// O documento foi carregado, mas ainda não foi processado.
        /// </summary>
        Uploaded,

        /// <summary>
        /// O texto do documento está a ser extraído.
        /// </summary>
        Processing,

        /// <summary>
        /// O documento foi processado com sucesso.
        /// </summary>
        Completed,

        /// <summary>
        /// O processamento terminou com erro.
        /// </summary>
        Failed
    }
}
