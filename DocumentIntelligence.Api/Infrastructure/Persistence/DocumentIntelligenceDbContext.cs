using DocumentIntelligence.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocumentIntelligence.Api.Infrastructure.Persistence
{
    /// <summary>
    /// Representa a sessão de comunicação entre a aplicação
    /// e a base de dados PostgreSQL.
    /// </summary>
    public class DocumentIntelligenceDbContext : DbContext
    {
        /// <summary>
        /// Cria uma nova instância do contexto utilizando as opções
        /// configuradas no Program.cs, incluindo o provider Npgsql
        /// e a connection string do PostgreSQL.
        /// </summary>
        /// <param name="options">
        /// Opções de configuração do Entity Framework Core.
        /// </param>
        public DocumentIntelligenceDbContext(DbContextOptions<DocumentIntelligenceDbContext> options) : base(options)
        {  
        }

        /// <summary>
        /// Representa o conjunto de documentos armazenados na base de dados.
        /// Permite consultar, adicionar, alterar e remover documentos.
        /// </summary>
        public DbSet<Document> Documents => Set<Document>();

        /// <summary>
        /// Aplica automaticamente as configurações das entidades
        /// existentes neste projeto.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocumentIntelligenceDbContext).Assembly);
        }
    }
}
