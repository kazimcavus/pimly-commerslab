using Pimly.ProductPublications.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddPimlyProductPublicationsWorker(builder.Configuration);

var host = builder.Build();
host.Run();
