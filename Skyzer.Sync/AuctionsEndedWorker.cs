using MongoDB.Driver;
using Skyzer.Shared.Models;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Skyzer.Sync
{
    public class AuctionsEndedWorker(ILogger<AuctionsEndedWorker> logger, IMongoDatabase database) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var endedCyclesCollection = database.GetCollection<Cycle>("ended_auctions_cycles");
            var endedAuctionsCollection = database.GetCollection<EndedAuction>("ended_auctions");
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
                    var page = await client.GetFromJsonAsync<EndedAuctionsResponse>(Constants.ENDED_AUCTIONS_URL, stoppingToken);

                    if (page == null)
                    {
                        logger.LogError("page was null!");
                        sleepTime = TimeSpan.FromSeconds(2);
                        continue;
                    }

                    if (!page.Success)
                    {
                        logger.LogWarning("hypixel returned success false");
                        sleepTime = Helper.TimeToWait(page.LastUpdated).Add(TimeSpan.FromSeconds(2));
                        continue;
                    }

                    logger.LogInformation("Starting cycle {}", page.LastUpdated);

                    if (await Helper.IsCycleProcessed(endedCyclesCollection, page.LastUpdated, stoppingToken))
                    {
                        logger.LogInformation("Cycle {Cycle} was already processed, adding a slight delay.", page.LastUpdated);

                        // if the cycle has already been processed, wait for the next cycle plus a little delay
                        sleepTime = Helper.TimeToWait(page.LastUpdated).Add(TimeSpan.FromSeconds(2));
                        continue;
                    }

                    var endedAuctions = page.Auctions.Where(a => a.AuctionId != null).ToList();

                    // Store every ended auction (BIN and regular) as its own sale, even if we never saw it
                    // while it was active. Auctions sniped within a single cycle only show up here.
                    var saleModels = endedAuctions
                        .Select(ended_auction => new ReplaceOneModel<EndedAuction>(
                            Builders<EndedAuction>.Filter.Eq(e => e.AuctionId, ended_auction.AuctionId),
                            ended_auction) { IsUpsert = true })
                        .ToList<WriteModel<EndedAuction>>();

                    // BulkWriteAsync throws when given no models
                    if (saleModels.Count > 0)
                        await endedAuctionsCollection.BulkWriteAsync(saleModels, new BulkWriteOptions { IsOrdered = false }, stoppingToken);

                    // Update the BIN auctions we already have. Auctions we never saw are not matched and are skipped,
                    // AddToSet keeps retried cycles from adding duplicates.
                    var auctionModels = endedAuctions
                        .Where(ended_auction => ended_auction.Bin)
                        .Select(ended_auction =>
                        {
                            var filter = Builders<Auction>.Filter.Eq(a => a.Uuid, ended_auction.AuctionId);

                            var winningBid = new Bid
                            {
                                AuctionId = ended_auction.AuctionId,
                                Bidder = ended_auction.Buyer,
                                ProfileId = ended_auction.BuyerProfile,
                                Amount = ended_auction.Price,
                                Timestamp = ended_auction.Timestamp
                            };

                            var update = Builders<Auction>.Update
                                // Update auction to claimed
                                .Set(a => a.Claimed, true)
                                // Add buyer to claim bidders
                                .AddToSet(a => a.ClaimedBidders, ended_auction.Buyer)
                                // Add price paid to highest bid amount
                                .Set(a => a.HighestBidAmount, ended_auction.Price)
                                // Update the last time the auction was updated
                                .Set(a => a.LastUpdated, ended_auction.Timestamp)
                                // Add the winning bid to the bids
                                .AddToSet(a => a.Bids, winningBid);

                            return new UpdateOneModel<Auction>(filter, update);
                        })
                        .ToList<WriteModel<Auction>>();

                    if (auctionModels.Count > 0)
                        await auctionsCollection.BulkWriteAsync(auctionModels, new BulkWriteOptions { IsOrdered = false }, stoppingToken);

                    // only mark the cycle as processed once it is written, so a crash mid-cycle gets retried
                    await Helper.ProcessCycle(endedCyclesCollection, page.LastUpdated, stoppingToken);

                    sleepTime = Helper.TimeToWait(page.LastUpdated);

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
