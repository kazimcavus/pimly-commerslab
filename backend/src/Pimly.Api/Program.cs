using Catalog.Api;
using Catalog.Application;
using Catalog.Infrastructure;
using Pimly.Api.ExceptionHandling;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCatalogApplication();
builder.Services.AddCatalogInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await app.Services.ApplyCatalogMigrationsAsync(app.Configuration);

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));
app.MapCatalogEndpoints();

app.Run();

/// <summary>Pimly API uygulamasının giriş noktası.</summary>
public partial class Program;
