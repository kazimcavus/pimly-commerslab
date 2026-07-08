using Pimly.Catalog.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddCatalogOutboxWorker(builder.Configuration);

var host = builder.Build();

await host.RunAsync();
