using MongoDB.Driver;
using Skyzer.Sync;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IMongoClient>(
    new MongoClient(builder.Configuration.GetConnectionString("MongoDB")));
builder.Services.AddSingleton<IMongoDatabase>(
    sp => sp.GetRequiredService<IMongoClient>().GetDatabase("skyblock"));

builder.Services.AddHostedService<ActiveAuctionsWorker>();
builder.Services.AddHostedService<AuctionsEndedWorker>();

var host = builder.Build();

await Indexes.EnsureAsync(host.Services.GetRequiredService<IMongoDatabase>(), default);

host.Run();
