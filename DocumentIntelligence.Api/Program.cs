using DocumentIntelligence.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using DocumentIntelligence.Api.Application.Abstractions.Storage;
using DocumentIntelligence.Api.Infrastructure.Storage;
using DocumentIntelligence.Api.Application.Abstractions.Documents;
using DocumentIntelligence.Api.Infrastructure.Documents.Extraction;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Obtém a connection string configurada nos User Secrets.
// A aplicação não deve arrancar sem saber como ligar ao PostgreSQL.
var connectionString = builder.Configuration.GetConnectionString("PostgreSql")
                       ?? throw new InvalidOperationException("A connection string 'PostgreSql' não está configurada.");

// Add services to the container.
// Regista o DbContext e configura o PostgreSQL através do provider Npgsql.
builder.Services.AddDbContext<DocumentIntelligenceDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// Regista o serviço responsável pelo armazenamento dos ficheiros.
builder.Services.AddScoped<IDocumentStorage, LocalDocumentStorage>();

// Regista o extrator utilizado para obter texto dos ficheiros PDF.
builder.Services.AddScoped<IDocumentTextExtractor, PdfPigDocumentTextExtractor>();

// Adiciona suporte para controllers da API.
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// Gera a documentação OpenAPI da aplicação.
builder.Services.AddOpenApi();

// Adiciona os health checks e verifica se a aplicação
// consegue estabelecer ligação ao PostgreSQL através do DbContext.
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<DocumentIntelligenceDbContext>(
        name: "postgresql",
        tags: ["database", "ready"]
    );   

var app = builder.Build();

// Configure the HTTP request pipeline.
// O endpoint OpenAPI fica disponível apenas em desenvolvimento.
if (app.Environment.IsDevelopment())
{
    // Disponibiliza o documento OpenAPI em /openapi/v1.json
    app.MapOpenApi();

    // Disponibiliza uma interface gráfica para explorar e testar a API
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// Endpoint utilizado para verificar o estado da aplicação.
app.MapHealthChecks("/health");

// Disponibiliza os endpoints definidos nos controllers.
app.MapControllers();

app.Run();
