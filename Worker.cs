using MongoDB.Driver;
using SkyzerSync.Models;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection.Metadata;

namespace SkyzerSync
{
    public class Worker(ILogger<Worker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Set up Mongo
            // TODO: grab from user secrets instead of hardcoding db string.
            var mongoClient = new MongoClient("mongodb://localhost:27018");
            var db = mongoClient.GetDatabase("skyblock");
            var auctionsCollection = db.GetCollection<Auction>("current_auctions");
            var cyclesCollection = db.GetCollection<Cycle>("cycles");

            // Set up HttpClient
            using HttpClient client = new();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            while (!stoppingToken.IsCancellationRequested)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                int sleepTime = await ProcessActiveAuctions(client, auctionsCollection, cyclesCollection, Constants.ACTIVE_AUCTIONS_STARTING_PAGE, stoppingToken);
                stopwatch.Stop();
                logger.LogInformation("Took {Elapsed} seconds to sync cycle.", stopwatch.Elapsed);
                logger.LogInformation("Sleeping for {SleepTimeInSeconds} seconds", sleepTime / 1000);
                await Task.Delay(sleepTime, stoppingToken);
            }
        }

        /// <summary>
        /// Method to process the active auctions available through Hypixel's API.
        /// </summary>
        /// <param name="client">A configured HTTP client</param>
        /// <param name="auctionsCollection">A mongodb collection of type Auction</param>
        /// <param name="cyclesCollection">A mongodb collection of type long</param>
        /// <param name="currentPage">The current page of the Hypixel endpoint (used for recursion)</param>
        /// <returns></returns>
        private async Task<int> ProcessActiveAuctions(
            HttpClient client,
            IMongoCollection<Auction> auctionsCollection,
            IMongoCollection<Cycle> cyclesCollection,
            int currentPage,
            CancellationToken stoppingToken
        )
        {
            var pageStr = string.Empty;
            try
            {
                pageStr = await client.GetStringAsync(Constants.ACTIVE_AUCTIONS_URL + $"?page={currentPage}", stoppingToken);
            } 
            catch (HttpRequestException ex)
            {
                logger.LogError("request failed: {Ex}", ex.Message);
                return 0;
            }

            if (String.IsNullOrEmpty(pageStr))
            {
                logger.LogError("Hypixel API returned a null or empty string when grabbing active auctions");
                return 0;
            }

            var page = Newtonsoft.Json.JsonConvert.DeserializeObject<ActiveAuctionResponse>(pageStr);

            if (page == null)
            {
                logger.LogError("We were unable to deserialize the response from Hypixel API.");
                return 0;
            }

            if (!page.Success)
            {
                logger.LogWarning("Hypixel API returned no success.");
                return 0;
            }

            if (page.Page == 0)
            {
                if (await IsCycleProcessed(cyclesCollection, page.LastUpdated, stoppingToken))
                    return TimeToWait(page.LastUpdated);
                else
                    await ProcessCycle(cyclesCollection, page.LastUpdated, stoppingToken);

                logger.LogInformation("Starting cycle {Cycle}", page.LastUpdated);
            }

            if (page.Auctions!.Count == 0)
            {
                logger.LogError("Hypixel API returned no auctions.");
                return 0;
            }

            // sync every auction on this page to mongo
            foreach (Auction auction in page.Auctions)
            {
                // filter to the auction by uuid
                var filter = Builders<Auction>.Filter.Eq(a => a.Uuid, auction.Uuid);

                // this took me way too long to figure out: but you can call replace with isUpsert and it will create the document
                // if it doesn't already exist.
                await auctionsCollection.ReplaceOneAsync(filter, auction, new ReplaceOptions { IsUpsert = true }, stoppingToken);
            }

            // if this is the last page: stop
            if (currentPage == page.TotalPages - 1)
                return TimeToWait(page.LastUpdated);

            // grab the next page
            return await ProcessActiveAuctions(client, auctionsCollection, cyclesCollection, currentPage + 1, stoppingToken);
        }

        /// <summary>
        /// Method to determine if a cycle has already been processed.
        /// </summary>
        /// <param name="cyclesCollection">A mongodb collection of type Cycle</param>
        /// <param name="cycle">The LastUpdatedTime of the active auction page's response.</param>
        /// <returns>True if the cycle has already been processed.</returns>
        private static async Task<bool> IsCycleProcessed(IMongoCollection<Cycle> cyclesCollection, long cycle, CancellationToken stoppingToken)
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
        private static async Task ProcessCycle(IMongoCollection<Cycle> cyclesCollection, long cycle, CancellationToken stoppingToken)
        {
            await cyclesCollection.InsertOneAsync(new Cycle { Id = cycle }, cancellationToken: stoppingToken);
        }

        /// <summary>
        /// Helper method that calculates how long the worker should wait before
        /// checking Hypixel's active auctions API endpoint.
        /// </summary>
        /// <param name="lastUpdated">The last known update time from the endpoint.</param>
        /// <returns>How long to wait in milliseconds</returns>
        private static int TimeToWait(long lastUpdated)
        {
            long now = (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalMilliseconds;
            int timeToWait = (int)(now - lastUpdated);
            return timeToWait;
        }
    }
}
