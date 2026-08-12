using Channels.Infrastructure;
using Pimly.TaxonomySync.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddPimlyTaxonomySyncWorker(builder.Configuration);

var host = builder.Build();

await host.Services.ApplyChannelsMigrationsAsync(host.Services.GetRequiredService<IConfiguration>());

await host.RunAsync();
