using MongoDB.Driver;
using Skyzer.Shared.Models;

namespace Skyzer.Sync
{
    /// <summary>
    /// Creates the MongoDB indexes the sync, dashboard, and ML queries rely on.
    /// </summary>
    public static class Indexes
    {
        /// <summary>
        /// Ensures the indexes exist. Creating an index that already exists is a no-op, so this is safe
        /// to call on every startup. The first run against a large collection may take a while.
        /// </summary>
        /// <param name="database">The skyblock mongodb database.</param>
        public static async Task EnsureAsync(IMongoDatabase database, CancellationToken stoppingToken)
        {
            var auctionKeys = Builders<Auction>.IndexKeys;
            await database.GetCollection<Auction>("auctions").Indexes.CreateManyAsync(
            [
                // matches the sort used by the dashboard's AuctionService
                new CreateIndexModel<Auction>(auctionKeys.Descending(a => a.LastUpdated).Ascending(a => a.Uuid)),
                // lifecycle queries, e.g. new listings and listings that stopped being seen
                new CreateIndexModel<Auction>(auctionKeys.Ascending(a => a.LastSeen)),
            ], stoppingToken);

            await database.GetCollection<EndedAuction>("ended_auctions").Indexes.CreateOneAsync(
                new CreateIndexModel<EndedAuction>(Builders<EndedAuction>.IndexKeys.Descending(e => e.Timestamp)),
                cancellationToken: stoppingToken);
        }
    }
}
