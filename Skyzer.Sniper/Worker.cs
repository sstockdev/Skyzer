using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Skyzer.Shared.Models;

namespace Skyzer.Sniper
{
    /// <summary>
    /// The sniper worker. Each cycle it (1) builds a market-price reference from recently
    /// sold BIN auctions and (2) scans live BIN listings for ones priced below that
    /// reference, persisting the opportunities to the <c>snipes</c> collection.
    /// </summary>
    public class Worker(
        ILogger<Worker> logger,
        IMongoDatabase database,
        IOptions<SniperOptions> options) : BackgroundService
    {
        private readonly SniperOptions _options = options.Value;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var auctions = database.GetCollection<Auction>("auctions");
            var marketPrices = database.GetCollection<MarketPrice>("market_prices");
            var snipes = database.GetCollection<Snipe>("snipes");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var references = await BuildMarketReferenceAsync(auctions, marketPrices, stoppingToken);
                    logger.LogInformation("Built market reference for {Count} item keys.", references.Count);

                    var found = await DetectSnipesAsync(auctions, references, snipes, stoppingToken);
                    logger.LogInformation("Detected {Count} snipe opportunities.", found);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Sniper cycle failed.");
                }

                await Task.Delay(TimeSpan.FromSeconds(_options.CycleDelaySeconds), stoppingToken);
            }
        }

        /// <summary>
        /// Builds per-item-key price stats from BIN auctions sold within the configured
        /// window and upserts them into <c>market_prices</c>. Returns the references keyed
        /// by item key so the detection pass can reuse them without re-reading.
        /// </summary>
        private async Task<Dictionary<string, MarketPrice>> BuildMarketReferenceAsync(
            IMongoCollection<Auction> auctions,
            IMongoCollection<MarketPrice> marketPrices,
            CancellationToken stoppingToken)
        {
            long windowStart = DateTimeOffset.UtcNow
                .AddHours(-_options.MarketWindowHours)
                .ToUnixTimeMilliseconds();

            // A sold BIN auction: claimed, and updated (claimed) within the window.
            var soldFilter = Builders<Auction>.Filter.And(
                Builders<Auction>.Filter.Eq(a => a.Bin, true),
                Builders<Auction>.Filter.Eq(a => a.Claimed, true),
                Builders<Auction>.Filter.Gte(a => a.LastUpdated, windowStart));

            var sold = await auctions
                .Find(soldFilter)
                .Project(a => new { a.ItemName, a.Tier, a.HighestBidAmount, a.StartingBid })
                .ToListAsync(stoppingToken);

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var references = sold
                .GroupBy(s => ItemKey(s.ItemName, s.Tier))
                .Select(g =>
                {
                    // For a BIN sale the sale price is the starting bid; fall back to it when
                    // the ended-auction price did not land on the doc.
                    var prices = g
                        .Select(s => s.HighestBidAmount > 0 ? s.HighestBidAmount : s.StartingBid)
                        .Where(p => p > 0)
                        .OrderBy(p => p)
                        .ToList();

                    return new MarketPrice
                    {
                        ItemKey = g.Key,
                        ItemName = g.First().ItemName,
                        Tier = g.First().Tier,
                        Median = Percentile(prices, 0.50),
                        LowerPercentile = Percentile(prices, 0.25),
                        SampleCount = prices.Count,
                        ComputedAt = now,
                    };
                })
                .Where(m => m.SampleCount > 0)
                .ToDictionary(m => m.ItemKey);

            if (references.Count > 0)
            {
                var writes = references.Values.Select(m =>
                    new ReplaceOneModel<MarketPrice>(
                        Builders<MarketPrice>.Filter.Eq(x => x.ItemKey, m.ItemKey), m)
                    { IsUpsert = true });

                await marketPrices.BulkWriteAsync(writes, cancellationToken: stoppingToken);
            }

            return references;
        }

        /// <summary>
        /// Scans live, purchasable BIN listings for ones priced below their market reference
        /// by enough margin to clear the configured profit threshold, and upserts the matches
        /// into <c>snipes</c>. Also prunes opportunities whose auctions have since expired.
        /// </summary>
        private async Task<int> DetectSnipesAsync(
            IMongoCollection<Auction> auctions,
            Dictionary<string, MarketPrice> references,
            IMongoCollection<Snipe> snipes,
            CancellationToken stoppingToken)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // A live listing: BIN, not yet claimed, and not yet expired.
            var liveFilter = Builders<Auction>.Filter.And(
                Builders<Auction>.Filter.Eq(a => a.Bin, true),
                Builders<Auction>.Filter.Eq(a => a.Claimed, false),
                Builders<Auction>.Filter.Gt(a => a.End, now));

            var live = await auctions
                .Find(liveFilter)
                .Project(a => new { a.Uuid, a.ItemName, a.Tier, a.StartingBid, a.End })
                .ToListAsync(stoppingToken);

            var detected = new List<Snipe>();

            foreach (var listing in live)
            {
                if (listing.StartingBid <= 0)
                    continue;

                var key = ItemKey(listing.ItemName, listing.Tier);
                if (!references.TryGetValue(key, out var reference))
                    continue;

                if (reference.SampleCount < _options.MinSampleCount)
                    continue;

                // Must be listed far enough below the median to be worth it.
                if (listing.StartingBid > reference.Median * _options.MaxPriceFraction)
                    continue;

                long resale = (long)(reference.Median * (1 - _options.AuctionHouseTax));
                long profit = resale - listing.StartingBid;
                if (profit < _options.MinProfitCoins)
                    continue;

                detected.Add(new Snipe
                {
                    Uuid = listing.Uuid,
                    ItemKey = key,
                    ItemName = listing.ItemName,
                    Tier = listing.Tier,
                    ListedPrice = listing.StartingBid,
                    ReferencePrice = reference.Median,
                    EstimatedProfit = profit,
                    SampleCount = reference.SampleCount,
                    End = listing.End,
                    DetectedAt = now,
                });
            }

            if (detected.Count > 0)
            {
                var writes = detected.Select(s =>
                    new ReplaceOneModel<Snipe>(
                        Builders<Snipe>.Filter.Eq(x => x.Uuid, s.Uuid), s)
                    { IsUpsert = true });

                await snipes.BulkWriteAsync(writes, cancellationToken: stoppingToken);
            }

            // Drop opportunities that are no longer purchasable: bought (claimed),
            // expired, or gone from the auctions collection entirely.
            var snipeUuids = await snipes
                .Find(FilterDefinition<Snipe>.Empty)
                .Project(s => s.Uuid)
                .ToListAsync(stoppingToken);

            if (snipeUuids.Count > 0)
            {
                var stillBuyable = await auctions
                    .Find(Builders<Auction>.Filter.And(
                        Builders<Auction>.Filter.In(a => a.Uuid, snipeUuids),
                        Builders<Auction>.Filter.Eq(a => a.Claimed, false),
                        Builders<Auction>.Filter.Gt(a => a.End, now)))
                    .Project(a => a.Uuid)
                    .ToListAsync(stoppingToken);

                var stale = snipeUuids.Except(stillBuyable).ToList();
                if (stale.Count > 0)
                {
                    await snipes.DeleteManyAsync(
                        Builders<Snipe>.Filter.In(s => s.Uuid, stale), stoppingToken);
                }
            }

            return detected.Count;
        }

        /// <summary>
        /// The v1 item key. Lossy on purpose: name + tier only. NBT-aware keying
        /// (enchants, pet level, stars, reforge) is a later phase.
        /// </summary>
        private static string ItemKey(string? itemName, string? tier)
            => $"{itemName ?? "?"}|{tier ?? "?"}";

        /// <summary>
        /// Linear-interpolated percentile over an ascending-sorted list.
        /// </summary>
        private static long Percentile(IReadOnlyList<long> sorted, double p)
        {
            if (sorted.Count == 0)
                return 0;
            if (sorted.Count == 1)
                return sorted[0];

            double rank = p * (sorted.Count - 1);
            int lo = (int)Math.Floor(rank);
            int hi = (int)Math.Ceiling(rank);
            if (lo == hi)
                return sorted[lo];

            double frac = rank - lo;
            return (long)(sorted[lo] + (sorted[hi] - sorted[lo]) * frac);
        }
    }
}
