using MongoDB.Driver;
using Skyzer.Shared.Models;

namespace Skyzer.Dashboard.Services
{
    public class AuctionService(IMongoDatabase database)
    {
        private readonly IMongoCollection<Auction> _auctions = database.GetCollection<Auction>("auctions");

        public async Task<List<Auction>> GetAllAsync() =>
            await _auctions.Find(_ => true).ToListAsync();
    }
}
