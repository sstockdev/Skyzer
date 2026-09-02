using MongoDB.Driver;
using Skyzer.Sniper;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IMongoClient>(
    new MongoClient(builder.Configuration.GetConnectionString("MongoDB")));
builder.Services.AddSingleton<IMongoDatabase>(
    sp => sp.GetRequiredService<IMongoClient>().GetDatabase("skyblock"));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
