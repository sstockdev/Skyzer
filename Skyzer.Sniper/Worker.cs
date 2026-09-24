using MongoDB.Driver;

namespace Skyzer.Sniper
{
    public class Worker(ILogger<Worker> logger, IMongoDatabase database) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Starting sniper");

            }
        }
    }
}
