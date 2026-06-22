using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Catalog.Api;
using Catalog.Application;
using Catalog.Infrastructure;
using Identity.Api;
using Identity.Application;
using Identity.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Pimly.Api.ExceptionHandling;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCatalogApplication();
builder.Services.AddCatalogInfrastructure(builder.Configuration);
builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var jwtSecret = builder.Configuration["Identity:Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    jwtSecret = "pimly-insecure-dev-secret";
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            NameClaimType = JwtRegisteredClaimNames.Sub,
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

await app.Services.ApplyCatalogMigrationsAsync(app.Configuration);
await app.Services.ApplyIdentityMigrationsAsync(app.Configuration);

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));
app.MapCatalogEndpoints();
app.MapIdentityEndpoints();

app.Run();

/// <summary>Pimly API uygulamasının giriş noktası.</summary>
public partial class Program;
