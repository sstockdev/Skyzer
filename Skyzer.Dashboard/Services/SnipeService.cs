using MongoDB.Driver;
using Skyzer.Shared.Models;

namespace Skyzer.Dashboard.Services
{
    public class SnipeService(IMongoDatabase database)
    {
        private readonly IMongoCollection<Snipe> _snipes = database.GetCollection<Snipe>("snipes");

        public async Task<IReadOnlyList<Snipe>> GetTopSnipesAsync(
            int limit,
            CancellationToken cancellationToken = default)
        {
            return await _snipes
                .Find(FilterDefinition<Snipe>.Empty)
                .SortByDescending(snipe => snipe.EstimatedProfit)
                .Limit(limit)
                .ToListAsync(cancellationToken);
        }
    }
}
