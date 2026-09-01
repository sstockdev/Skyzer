using MongoDB.Driver;
using Skyzer.Sync.Models;
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
                    else
                        await Helper.ProcessCycle(endedCyclesCollection, page.LastUpdated, stoppingToken);

                    await Parallel.ForEachAsync(page.Auctions, async (ended_auction, stoppingToken) =>
                    {

                        if (!ended_auction.Bin)
                            return;

                        var auctionsCollection = database.GetCollection<Auction>("auctions");
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
                            .Push(a => a.ClaimedBidders, ended_auction.Buyer)
                            // Add price paid to highest bid amount
                            .Set(a => a.HighestBidAmount, ended_auction.Price)
                            // Update the last time the auction was updated
                            .Set(a => a.LastUpdated, ended_auction.Timestamp)
                            // Add the winning bid to the bids
                            .Push(a => a.Bids, winningBid);

                        try
                        {
                            await auctionsCollection.UpdateOneAsync(filter, update, cancellationToken: stoppingToken);
                        }
                        catch
                        {
                            // tried to update, if it was unable to find that auction we fail silently.
                            // We really only care about auctions that already exist
                        }
                    });

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
