using Skyzer.Shared;
using Skyzer.Sniper;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSkyzerMongo(builder.Configuration);

builder.Services.Configure<SniperOptions>(
    builder.Configuration.GetSection(SniperOptions.SectionName));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
