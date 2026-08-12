using Pimly.Outbox.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddPimlyOutboxWorker(builder.Configuration);

var host = builder.Build();

await host.RunAsync();
