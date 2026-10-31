using MongoDB.Bson;
using MongoDB.Driver;
using Skyzer.Shared.Models;
using Skyzer.Sync.Nbt;
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

                    LogDecodeFailures(await UpsertPageAsync(auctionsCollection, firstPage.Auctions, cycle, stoppingToken), firstPage.Page);

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

                            LogDecodeFailures(await UpsertPageAsync(auctionsCollection, page.Auctions, cycle, stoppingToken), i);
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
        /// Logs one warning for a page if any of its auctions' items failed to decode.
        /// </summary>
        private void LogDecodeFailures(int failures, int page)
        {
            if (failures > 0)
                logger.LogWarning("{Count} auctions failed to decode on page {Page}", failures, page);
        }

        /// <summary>
        /// Upserts a page of auctions in a single bulk write. Only BIN auctions are stored.
        /// </summary>
        /// <param name="auctionsCollection">The mongodb auctions collection.</param>
        /// <param name="auctions">The auctions from a page of the active auctions API response.</param>
        /// <param name="cycle">The LastUpdatedTime of the active auction page's response.</param>
        /// <returns>The number of new BIN auctions on the page whose item failed to decode.</returns>
        private static async Task<int> UpsertPageAsync(IMongoCollection<Auction> auctionsCollection, IEnumerable<Auction> auctions, long cycle, CancellationToken stoppingToken)
        {
            var models = new List<WriteModel<Auction>>();
            var decodeFailures = 0;

            // we don't care about non buy it now auctions
            var binAuctions = auctions.Where(a => a.Bin).ToList();

            // only new auctions need their item decoded and sent, the item of a stored auction never changes
            var uuids = binAuctions.Select(a => a.Uuid).ToList();
            var existing = (await auctionsCollection
                .Find(Builders<Auction>.Filter.In(a => a.Uuid, uuids))
                .Project(a => a.Uuid)
                .ToListAsync(stoppingToken)).ToHashSet();

            foreach (var auction in binAuctions)
            {
                models.Add(BuildUpsert(auction, cycle, isNew: !existing.Contains(auction.Uuid)));

                if (auction.Item?.DecodeError != null)
                    decodeFailures++;
            }

            // BulkWriteAsync throws when given no models
            if (models.Count == 0)
                return decodeFailures;

            await auctionsCollection.BulkWriteAsync(models, new BulkWriteOptions { IsOrdered = false }, stoppingToken);
            return decodeFailures;
        }

        /// <summary>
        /// Builds the auction's upsert. Sets <see cref="Auction.LastSeen"/> to the cycle every time.
        /// <see cref="Auction.FirstSeen"/> is only written when the auction is inserted for the first time.
        /// For new auctions the item is decoded and <see cref="Auction.ItemBytes"/> and <see cref="Auction.Item"/>
        /// are sent as insert-only fields. For auctions already stored they are not sent at all, since an
        /// auction's item never changes.
        /// </summary>
        /// <param name="auction">A BIN auction from the active auctions API response.</param>
        /// <param name="cycle">The LastUpdatedTime of the active auction page's response.</param>
        /// <param name="isNew">Whether the auction is not yet stored in the auctions collection.</param>
        internal static UpdateOneModel<Auction> BuildUpsert(Auction auction, long cycle, bool isNew)
        {
            if (isNew)
                auction.Item = ItemDecoder.Decode(auction.ItemBytes);

            var fields = auction.ToBsonDocument();
            fields.Remove("_id");
            fields.Remove(nameof(Auction.FirstSeen));
            fields.Remove(nameof(Auction.LastSeen));
            fields.Remove(nameof(Auction.ItemBytes));
            fields.Remove(nameof(Auction.Item));
            fields[nameof(Auction.LastSeen)] = cycle;

            // insert-only fields must not also be in $set, otherwise mongo rejects the update with a conflicting path error
            var insertOnly = new BsonDocument(nameof(Auction.FirstSeen), cycle);
            if (isNew)
            {
                if (auction.ItemBytes != null)
                    insertOnly[nameof(Auction.ItemBytes)] = auction.ItemBytes;
                insertOnly[nameof(Auction.Item)] = auction.Item.ToBsonDocument();
            }

            var update = new BsonDocument
            {
                { "$set", fields },
                { "$setOnInsert", insertOnly }
            };

            var filter = Builders<Auction>.Filter.Eq(a => a.Uuid, auction.Uuid);
            return new UpdateOneModel<Auction>(filter, update) { IsUpsert = true };
        }
    }
}
