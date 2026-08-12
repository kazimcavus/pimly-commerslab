using Pimly.ListingSync.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddPimlyListingSyncWorker(builder.Configuration);

var host = builder.Build();
host.Run();
