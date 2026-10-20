using MongoDB.Driver;
using Skyzer.Shared.Models;

namespace Skyzer.Dashboard.Services
{
    public class OverviewService(IMongoDatabase database)
    {
        private readonly IMongoCollection<Auction> _auctions = database.GetCollection<Auction>("auctions");
        private readonly IMongoCollection<Snipe> _snipes = database.GetCollection<Snipe>("snipes");
        private readonly IMongoCollection<MarketPrice> _marketPrices = database.GetCollection<MarketPrice>("market_prices");

        public async Task<OverviewStats> GetStatsAsync(CancellationToken cancellationToken = default)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long dayAgo = DateTimeOffset.UtcNow.AddHours(-24).ToUnixTimeMilliseconds();

            // Cheap collection-level estimate for the headline total.
            var totalAuctions = await _auctions.EstimatedDocumentCountAsync(
                cancellationToken: cancellationToken);

            var liveFilter = Builders<Auction>.Filter.And(
                Builders<Auction>.Filter.Eq(a => a.Claimed, false),
                Builders<Auction>.Filter.Gt(a => a.End, now));
            var liveAuctions = await _auctions.CountDocumentsAsync(
                liveFilter, cancellationToken: cancellationToken);

            var soldFilter = Builders<Auction>.Filter.And(
                Builders<Auction>.Filter.Eq(a => a.Claimed, true),
                Builders<Auction>.Filter.Gte(a => a.LastUpdated, dayAgo));
            var soldLast24h = await _auctions.CountDocumentsAsync(
                soldFilter, cancellationToken: cancellationToken);

            var activeSnipes = await _snipes.EstimatedDocumentCountAsync(
                cancellationToken: cancellationToken);
            var marketPriceKeys = await _marketPrices.EstimatedDocumentCountAsync(
                cancellationToken: cancellationToken);

            // Freshness: newest LastUpdated across stored auctions (0 when empty).
            var newestUpdate = await _auctions
                .Find(FilterDefinition<Auction>.Empty)
                .SortByDescending(a => a.LastUpdated)
                .Limit(1)
                .Project(a => a.LastUpdated)
                .FirstOrDefaultAsync(cancellationToken);

            return new OverviewStats(
                totalAuctions,
                liveAuctions,
                soldLast24h,
                activeSnipes,
                marketPriceKeys,
                newestUpdate);
        }
    }

    public sealed record OverviewStats(
        long TotalAuctions,
        long LiveAuctions,
        long SoldLast24h,
        long ActiveSnipes,
        long MarketPriceKeys,
        long NewestUpdate);
}
