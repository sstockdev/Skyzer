using MongoDB.Driver;
using Skyzer.Sync.Models;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Skyzer.Sync
{
    public class ActiveAuctionsWorker(ILogger<ActiveAuctionsWorker> logger, IMongoDatabase database) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var cyclesCollection = database.GetCollection<Cycle>("active_auctions_cycles");
            TimeSpan sleepTime = new();

            while (!stoppingToken.IsCancellationRequested)
            {
                if (sleepTime > TimeSpan.Zero)
                {
                    logger.LogInformation("Sleeping for {SleepTimeInSeconds} seconds", sleepTime.TotalSeconds);
                    await Task.Delay(sleepTime, stoppingToken);
                }

                Stopwatch stopwatch = new();
                stopwatch.Start();

                try
                {
                    var firstPage = await client.GetFromJsonAsync<ActiveAuctionResponse>(Constants.ACTIVE_AUCTIONS_URL, stoppingToken);
                    if (firstPage == null || firstPage.Auctions == null)
                    {
                        logger.LogError("firstPage or firstPage auctions were was null!");
                        sleepTime = TimeSpan.FromSeconds(2);
                        continue;
                    }

                    logger.LogInformation("Starting cycle {}", firstPage.LastUpdated);

                    if (await Helper.IsCycleProcessed(cyclesCollection, firstPage.LastUpdated, stoppingToken))
                    {
                        logger.LogInformation("Cycle {Cycle} was already processed, adding a slight delay.", firstPage.LastUpdated);

                        // if the cycle has already been processed, wait for the next cycle plus a little delay
                        sleepTime = Helper.TimeToWait(firstPage.LastUpdated).Add(TimeSpan.FromSeconds(2)) ;
                        continue;
                    }
                    else
                        await Helper.ProcessCycle(cyclesCollection, firstPage.LastUpdated, stoppingToken);

                    await Parallel.ForEachAsync(firstPage.Auctions, async (auction, stoppingToken) =>
                    {
                        var auctionsCollection = database.GetCollection<Auction>("auctions");

                        // we don't care about non buy it now auctions
                        if (!auction.Bin)
                            return;

                        // filter to the auction by uuid
                        var filter = Builders<Auction>.Filter.Eq(a => a.Uuid, auction.Uuid);

                        // this took me way too long to figure out: but you can call replace with isUpsert and it will create the document
                        // if it doesn't already exist.
                        await auctionsCollection.ReplaceOneAsync(filter, auction, new ReplaceOptions { IsUpsert = true }, stoppingToken);
                    });

                    await Parallel.ForAsync(firstPage.Page + 1, firstPage.TotalPages, async (i, stoppingToken) =>
                    {
                        var auctionsCollection = database.GetCollection<Auction>("auctions");

                        try
                        {
                            var page = await client.GetFromJsonAsync<ActiveAuctionResponse>(Constants.ACTIVE_AUCTIONS_URL + $"?page={i}", stoppingToken);
                            if (page == null || page.Auctions == null)
                            {
                                logger.LogError("page or auctions was null!");
                                return; // continue; equivalent for Parallel.ForAsync
                            }

                            await Parallel.ForEachAsync(page.Auctions, async (auction, stoppingToken) =>
                            {

                                if (!auction.Bin)
                                    return;

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
                        catch (System.TimeoutException ex)
                        {
                            logger.LogError(ex.Message + "\nCheck DB connection!");
                            return; // continue;
                        }
                    });

                    sleepTime = Helper.TimeToWait(firstPage.LastUpdated);
                }
                catch (HttpRequestException ex)
                {
                    logger.LogError(ex.Message);
                    sleepTime = TimeSpan.FromSeconds(2);
                    continue;
                }

                stopwatch.Stop();
                logger.LogInformation("Took {Elapsed} seconds to sync cycle.", stopwatch.Elapsed.TotalSeconds);
            }
        }
    }
}
