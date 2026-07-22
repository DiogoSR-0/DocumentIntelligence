using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentIntelligence.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentProcessingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "extracted_text",
                table: "documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "page_count",
                table: "documents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "processing_error",
                table: "documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "documents",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Uploaded");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "extracted_text",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "page_count",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "processing_error",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "status",
                table: "documents");
        }
    }
}
