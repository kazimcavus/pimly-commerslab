using Catalog.Infrastructure;
using Channels.Infrastructure;
using Pimly.ProductImports.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddPimlyProductImportsWorker(builder.Configuration);

var host = builder.Build();

await host.Services.ApplyChannelsMigrationsAsync(host.Services.GetRequiredService<IConfiguration>());
await host.Services.ApplyCatalogMigrationsAsync(host.Services.GetRequiredService<IConfiguration>());

await host.RunAsync();
