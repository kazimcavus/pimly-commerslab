using Channels.Application;
using Channels.Infrastructure;
using Pimly.Channels.Worker;
using Pimly.Channels.Worker.Taxonomy;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddChannelsApplication();
builder.Services.AddChannelsInfrastructure(builder.Configuration);
builder.Services.AddHostedService<TaxonomySyncBackgroundService>();
builder.Services.AddHostedService<ScheduledTaxonomySyncBackgroundService>();

var host = builder.Build();

await host.Services.ApplyChannelsMigrationsAsync(host.Services.GetRequiredService<IConfiguration>());

await host.RunAsync();
