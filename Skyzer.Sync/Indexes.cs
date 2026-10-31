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
                // per-item queries on the decoded item
                new CreateIndexModel<Auction>(auctionKeys.Ascending(a => a.Item!.SkyblockId)),
            ], stoppingToken);

            var endedKeys = Builders<EndedAuction>.IndexKeys;
            await database.GetCollection<EndedAuction>("ended_auctions").Indexes.CreateManyAsync(
            [
                new CreateIndexModel<EndedAuction>(endedKeys.Descending(e => e.Timestamp)),
                // recent sales of a given item, e.g. for training data
                new CreateIndexModel<EndedAuction>(endedKeys.Ascending(e => e.Item!.SkyblockId).Descending(e => e.Timestamp)),
            ], stoppingToken);
        }
    }
}
