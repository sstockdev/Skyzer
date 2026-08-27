using MongoDB.Driver;
using SkyzerSync.Models;

namespace SkyzerSync
{
    /// <summary>
    /// Helper class that has shared methods
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// Method to determine if a cycle has already been processed.
        /// </summary>
        /// <param name="cyclesCollection">A mongodb collection of type Cycle</param>
        /// <param name="cycle">The LastUpdatedTime of the active auction page's response.</param>
        /// <returns>True if the cycle has already been processed.</returns>
        public static async Task<bool> IsCycleProcessed(IMongoCollection<Cycle> cyclesCollection, long cycle, CancellationToken stoppingToken)
        {
            var filter = Builders<Cycle>.Filter.Eq(c => c.Id, cycle);
            var result = await cyclesCollection.Find(filter).FirstOrDefaultAsync();
            return result != null;
        }

        /// <summary>
        /// Method to process a cycle. It stores the cycle's Id in the mongo collection.
        /// </summary>
        /// <param name="cyclesCollection">A mongodb collection of type Cycle</param>
        /// <param name="cycle">The LastUpdatedTime of the active auction page's response.</param>
        public static async Task ProcessCycle(IMongoCollection<Cycle> cyclesCollection, long cycle, CancellationToken stoppingToken)
        {
            await cyclesCollection.InsertOneAsync(new Cycle { Id = cycle }, cancellationToken: stoppingToken);
        }

        /// <summary>
        /// Helper method that calculates how long the worker should wait before
        /// checking Hypixel's active auctions API endpoint.
        /// </summary>
        /// <param name="lastUpdated">The last known update time from the endpoint.</param>
        /// <returns>How long to wait in milliseconds</returns>
        public static int TimeToWait(long lastUpdated, long apiDelay)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // last updated + api delay = next update; next update - now is how long to wait
            long timeToWait = (lastUpdated + apiDelay) - now;
            return (int)timeToWait;
        }
    }
}
