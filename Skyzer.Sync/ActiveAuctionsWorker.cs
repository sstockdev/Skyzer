using MongoDB.Bson;
using MongoDB.Driver;
using Skyzer.Shared.Models;
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
            var auctionsCollection = database.GetCollection<Auction>("auctions");
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

                    var cycle = firstPage.LastUpdated;

                    await UpsertPageAsync(auctionsCollection, firstPage.Auctions, cycle, stoppingToken);

                    await Parallel.ForAsync(firstPage.Page + 1, firstPage.TotalPages, async (i, stoppingToken) =>
                    {
                        try
                        {
                            var page = await client.GetFromJsonAsync<ActiveAuctionResponse>(Constants.ACTIVE_AUCTIONS_URL + $"?page={i}", stoppingToken);
                            if (page == null || page.Auctions == null)
                            {
                                logger.LogError("page or auctions was null!");
                                return; // continue; equivalent for Parallel.ForAsync
                            }

                            await UpsertPageAsync(auctionsCollection, page.Auctions, cycle, stoppingToken);
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

                    // only mark the cycle as processed once its pages are written, so a crash mid-cycle
                    // gets retried. The upserts are idempotent so retrying is safe.
                    await Helper.ProcessCycle(cyclesCollection, cycle, stoppingToken);

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

        /// <summary>
        /// Upserts a page of auctions in a single bulk write. Only BIN auctions are stored.
        /// Sets <see cref="Auction.LastSeen"/> to the cycle every time, and <see cref="Auction.FirstSeen"/>
        /// only when the auction is inserted for the first time.
        /// </summary>
        /// <param name="auctionsCollection">The mongodb auctions collection.</param>
        /// <param name="auctions">The auctions from a page of the active auctions API response.</param>
        /// <param name="cycle">The LastUpdatedTime of the active auction page's response.</param>
        private static async Task UpsertPageAsync(IMongoCollection<Auction> auctionsCollection, IEnumerable<Auction> auctions, long cycle, CancellationToken stoppingToken)
        {
            var models = new List<WriteModel<Auction>>();

            foreach (var auction in auctions)
            {
                // we don't care about non buy it now auctions
                if (!auction.Bin)
                    continue;

                var fields = auction.ToBsonDocument();
                fields.Remove("_id");
                fields.Remove(nameof(Auction.FirstSeen));
                fields.Remove(nameof(Auction.LastSeen));
                fields[nameof(Auction.LastSeen)] = cycle;

                // FirstSeen must not also be in $set, otherwise mongo rejects the update with a conflicting path error
                var update = new BsonDocument
                {
                    { "$set", fields },
                    { "$setOnInsert", new BsonDocument(nameof(Auction.FirstSeen), cycle) }
                };

                var filter = Builders<Auction>.Filter.Eq(a => a.Uuid, auction.Uuid);
                models.Add(new UpdateOneModel<Auction>(filter, update) { IsUpsert = true });
            }

            // BulkWriteAsync throws when given no models
            if (models.Count == 0)
                return;

            await auctionsCollection.BulkWriteAsync(models, new BulkWriteOptions { IsOrdered = false }, stoppingToken);
        }
    }
}
