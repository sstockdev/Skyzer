using MongoDB.Driver;
using SkyzerSync.Models;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SkyzerSync
{
    public class ActiveAuctionsWorker(ILogger<ActiveAuctionsWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var cyclesCollection = new MongoClient("mongodb://localhost:27018").GetDatabase("skyblock").GetCollection<Cycle>("cycles");

            int sleepTime = -1;

            while (!stoppingToken.IsCancellationRequested)
            {
                if (sleepTime > 0)
                    await Task.Delay(sleepTime, stoppingToken);

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                try
                {
                    var firstPage = await client.GetFromJsonAsync<ActiveAuctionResponse>(Constants.ACTIVE_AUCTIONS_URL, stoppingToken);
                    if (firstPage == null)
                    {
                        logger.LogError("firstPage was null!");
                        sleepTime = 1000; // if the first page is null, wait 1 second before trying again
                        continue;
                    }

                    logger.LogInformation("Startin cycle {}", firstPage.LastUpdated);

                    if (await IsCycleProcessed(cyclesCollection, firstPage.LastUpdated, stoppingToken))
                    {
                        logger.LogInformation("Cycle {Cycle} was already processed, returning.", firstPage.LastUpdated);
                        sleepTime = TimeToWait(firstPage.LastUpdated); // if the cycle has already been processed, wait for the next cycle
                        continue;
                    }
                    else
                        await ProcessCycle(cyclesCollection, firstPage.LastUpdated, stoppingToken);

                    await Parallel.ForEachAsync(firstPage.Auctions, async (auction, stoppingToken) =>
                    {
                        var auctionsCollection = new MongoClient("mongodb://localhost:27018").GetDatabase("skyblock").GetCollection<Auction>("auctions");

                        // filter to the auction by uuid
                        var filter = Builders<Auction>.Filter.Eq(a => a.Uuid, auction.Uuid);

                        // this took me way too long to figure out: but you can call replace with isUpsert and it will create the document
                        // if it doesn't already exist.
                        await auctionsCollection.ReplaceOneAsync(filter, auction, new ReplaceOptions { IsUpsert = true }, stoppingToken);
                    });

                    await Parallel.ForAsync(firstPage.Page + 1, firstPage.TotalPages, async (i, stoppingToken) =>
                    {
                        var auctionsCollection = new MongoClient("mongodb://localhost:27018").GetDatabase("skyblock").GetCollection<Auction>("auctions");

                        try
                        {
                            var page = await client.GetFromJsonAsync<ActiveAuctionResponse>(Constants.ACTIVE_AUCTIONS_URL + $"?page={i}", stoppingToken);
                            if (page == null)
                            {
                                logger.LogError("page was null!");
                                return; // continue; equivalent for Parallel.ForAsync
                            }

                            await Parallel.ForEachAsync(page.Auctions, async (auction, stoppingToken) =>
                            {
                                // filter to the auction by uuid
                                var filter = Builders<Auction>.Filter.Eq(a => a.Uuid, auction.Uuid);

                                // this took me way too long to figure out: but you can call replace with isUpsert and it will create the document
                                // if it doesn't already exist.
                                await auctionsCollection.ReplaceOneAsync(filter, auction, new ReplaceOptions { IsUpsert = true }, stoppingToken);
                            });
                        }
                        catch (HttpRequestException ex)
                        {
                            logger.LogError(ex.Message);
                            return; // continue;
                        }
                    });

                    sleepTime = TimeToWait(firstPage.LastUpdated);
                }
                catch (HttpRequestException ex)
                {
                    logger.LogError(ex.Message);
                    sleepTime = 1000; // if the first page returns an exception, wait 1 second before trying again
                    continue;
                }

                stopwatch.Stop();
                logger.LogInformation("Took {Elapsed} seconds to sync cycle.", stopwatch.Elapsed);
                logger.LogInformation("Sleeping for {SleepTimeInSeconds} seconds", sleepTime / 1000);
            }
        }

        /// <summary>
        /// Method to determine if a cycle has already been processed.
        /// </summary>
        /// <param name="cyclesCollection">A mongodb collection of type Cycle</param>
        /// <param name="cycle">The LastUpdatedTime of the active auction page's response.</param>
        /// <returns>True if the cycle has already been processed.</returns>
        private static async Task<bool> IsCycleProcessed(IMongoCollection<Cycle> cyclesCollection, long cycle, CancellationToken stoppingToken)
        {
            var filter = Builders<Cycle>.Filter.Eq(c => c.Id, cycle);
            var result = await cyclesCollection.Find(filter).FirstOrDefaultAsync();
            return result != null;
        }

        /// <summary>
        /// Method to process a cycle. It stores the cycle's Id in the mongo collection.
        /// </summary>
        /// <param name="cyclesCollection">A mongodb collection of type Cycle</param>
        /// <param name="cycle">The LastUpdatedTime of the active auction page's response.</param>
        private static async Task ProcessCycle(IMongoCollection<Cycle> cyclesCollection, long cycle, CancellationToken stoppingToken)
        {
            await cyclesCollection.InsertOneAsync(new Cycle { Id = cycle }, cancellationToken: stoppingToken);
        }

        /// <summary>
        /// Helper method that calculates how long the worker should wait before
        /// checking Hypixel's active auctions API endpoint.
        /// </summary>
        /// <param name="lastUpdated">The last known update time from the endpoint.</param>
        /// <returns>How long to wait in milliseconds</returns>
        private static int TimeToWait(long lastUpdated)
        {
            long now = (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalMilliseconds;
            int timeToWait = (int)(now - lastUpdated);
            return timeToWait;
        }
    }
}
