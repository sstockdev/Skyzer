using MongoDB.Driver;
using Skyzer.Shared;
using Skyzer.Sync;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSkyzerMongo(builder.Configuration);

builder.Services.AddHostedService<ActiveAuctionsWorker>();
builder.Services.AddHostedService<AuctionsEndedWorker>();

var host = builder.Build();

await Indexes.EnsureAsync(host.Services.GetRequiredService<IMongoDatabase>(), default);

host.Run();
