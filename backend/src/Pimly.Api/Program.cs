using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using Catalog.Api;
using Catalog.Application;
using Catalog.Infrastructure;
using Channels.Api;
using Channels.Application;
using Channels.Infrastructure;
using Identity.Api;
using Identity.Application;
using Identity.Infrastructure;
using Inventory.Api;
using Inventory.Application;
using Inventory.Infrastructure;
using Media.Api;
using Media.Application;
using Media.Application.Options;
using Media.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Pimly.Api.ExceptionHandling;
using Pimly.AspNetCore.Observability;
using Pimly.AspNetCore.Tenancy;
using Pimly.Integration;
using Pricing.Api;
using Pricing.Application;
using Pricing.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddPimlyObservability();

builder.Services.AddCatalogApplication();
builder.Services.AddCatalogInfrastructure(builder.Configuration);
builder.Services.AddChannelsApplication();
builder.Services.AddChannelsInfrastructure(builder.Configuration);
builder.Services.AddPricingApplication();
builder.Services.AddPricingInfrastructure(builder.Configuration);
builder.Services.AddInventoryApplication();
builder.Services.AddInventoryInfrastructure(builder.Configuration);
builder.Services.AddCatalogReadGateways();
builder.Services.AddProductItemExistenceGateways();
builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddMediaApplication();
builder.Services.AddMediaInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Web istemcisiyle (snake_case) uyumlu tel formatı. Minimal API endpoint'lerinin
// JSON serileştirmesini hem istek hem yanıt için snake_case'e ayarlar; modüllerdeki
// mevcut [JsonPropertyName("...")] öznitelikleriyle de tutarlıdır.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
});

var jwtSecret = builder.Configuration["Identity:Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    jwtSecret = "pimly-insecure-dev-secret-min-32-bytes-long";
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
builder.Services.AddPimlyTenancy();

var app = builder.Build();

await app.Services.ApplyCatalogMigrationsAsync(app.Configuration);
await app.Services.ApplyPricingMigrationsAsync(app.Configuration);
await app.Services.ApplyInventoryMigrationsAsync(app.Configuration);
await app.Services.ApplyChannelsMigrationsAsync(app.Configuration);
await app.Services.ApplyIdentityMigrationsAsync(app.Configuration);

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    await app.Services.SeedIdentityDevUserAsync();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UsePimlyObservability();

app.UseAuthentication();
app.UseAuthorization();

var mediaStoragePath = Path.GetFullPath(
    builder.Configuration.GetSection(MediaOptions.SectionName).GetValue<string>("StoragePath")
        ?? "./storage/media");
Directory.CreateDirectory(mediaStoragePath);
app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = "/media",
    FileProvider = new PhysicalFileProvider(mediaStoragePath),
});

app.MapCatalogEndpoints();
app.MapPricingEndpoints();
app.MapInventoryEndpoints();
app.MapChannelsEndpoints();
app.MapIdentityEndpoints();
app.MapMediaEndpoints();

app.Run();

/// <summary>Pimly API uygulamasının giriş noktası.</summary>
public partial class Program;
