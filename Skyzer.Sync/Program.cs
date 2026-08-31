using Skyzer.Sync;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<ActiveAuctionsWorker>();
builder.Services.AddHostedService<AuctionsEndedWorker>();

var host = builder.Build();
host.Run();
