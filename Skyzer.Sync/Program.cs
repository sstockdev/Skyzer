using Skyzer.Shared;
using Skyzer.Sync;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSkyzerMongo(builder.Configuration);

builder.Services.AddHostedService<ActiveAuctionsWorker>();
builder.Services.AddHostedService<AuctionsEndedWorker>();

var host = builder.Build();
host.Run();
