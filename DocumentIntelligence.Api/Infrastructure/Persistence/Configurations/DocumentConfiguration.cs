using DocumentIntelligence.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentIntelligence.Api.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Define como a entidade Document será armazenada no PostgreSQL.
    /// </summary>
    public sealed class DocumentConfiguration: IEntityTypeConfiguration<Document>
    {
        public void Configure(EntityTypeBuilder<Document> builder) 
        {
            //Define o nome da tabela no PostgreSQL
            builder.ToTable("documents");

            //Defina a chave primaria
            builder.HasKey(document  => document.Id);

            //Propriedades
            builder.Property(document => document.Id)
                .HasColumnName("id");

            builder.Property(document => document.FileName)
                .HasColumnName("file_name")
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(document => document.ContentType)
                .HasColumnName("content_type")
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(document => document.SizeBytes)
                .HasColumnName("syze_bytes")
                .IsRequired();

            builder.Property(document => document.StorageKey)
                .HasColumnName("storage_key")
                .HasMaxLength(500);

            builder.Property(document => document.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();
        }

    }
}
