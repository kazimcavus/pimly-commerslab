using Catalog.Infrastructure;
using Channels.Infrastructure;
using Pimly.Channels.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddPimlyWorker(builder.Configuration);

var host = builder.Build();

await host.Services.ApplyChannelsMigrationsAsync(host.Services.GetRequiredService<IConfiguration>());
await host.Services.ApplyCatalogMigrationsAsync(host.Services.GetRequiredService<IConfiguration>());

await host.RunAsync();
