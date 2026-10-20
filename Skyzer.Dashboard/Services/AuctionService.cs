using MongoDB.Driver;
using Skyzer.Shared.Models;

namespace Skyzer.Dashboard.Services
{
    public class AuctionService(IMongoDatabase database)
    {
        private readonly IMongoCollection<Auction> _auctions = database.GetCollection<Auction>("auctions");

        public async Task<AuctionPage> GetPageAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            // Collection-level estimate from metadata; avoids a full scan on every page request.
            var totalCount = await _auctions.EstimatedDocumentCountAsync(
                cancellationToken: cancellationToken);

            var auctions = await _auctions
                .Find(FilterDefinition<Auction>.Empty)
                .SortByDescending(auction => auction.LastUpdated)
                .ThenBy(auction => auction.Uuid)
                .Skip(page * pageSize)
                .Limit(pageSize)
                .ToListAsync(cancellationToken);

            return new AuctionPage(auctions, (int)totalCount);
        }
    }

    public sealed record AuctionPage(
        IReadOnlyList<Auction> Items,
        int TotalCount);
}
