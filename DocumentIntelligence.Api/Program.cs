using DocumentIntelligence.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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

// Adiciona suporte para controllers da API.
builder.Services.AddControllers();

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
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// Endpoint utilizado para verificar o estado da aplicação.
app.MapHealthChecks("/health");

// Disponibiliza os endpoints definidos nos controllers.
app.MapControllers();

app.Run();
