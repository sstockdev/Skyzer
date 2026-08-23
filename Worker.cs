using MongoDB.Driver;
using SkyzerSync.Models;
using System.Diagnostics;
using System.Net.Http.Headers;

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
            var collection = db.GetCollection<Auction>("current_auctions");

            // Set up HttpClient
            using HttpClient client = new();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            while (!stoppingToken.IsCancellationRequested)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                int sleepTime = await ProcessActiveAuctions(client, collection, Constants.ACTIVE_AUCTIONS_STARTING_PAGE, stoppingToken);
                stopwatch.Stop();
                logger.LogInformation("Took {Elapsed} seconds to sync cycle.", stopwatch.Elapsed);
                // TODO: instead of a hard coded delay, it should check if this cycle has already been processed (mongo) and wait a little bit extra if it has already.
                await Task.Delay(sleepTime + Constants.ACTIVE_AUCTIONS_TASK_DELAY, stoppingToken);
            }
        }

        private async Task ProcessCycle(long cycle, CancellationToken stoppingToken)
        {

        }

        /// <summary>
        /// Method to process the active auctions available through Hypixel's API.
        /// </summary>
        /// <param name="client">A configured HTTP client</param>
        /// <param name="collection">A mongodb collection of type Auction</param>
        /// <param name="currentPage">The current page of the Hypixel endpoint (used for recursion)</param>
        /// <returns></returns>
        private async Task<int> ProcessActiveAuctions(HttpClient client, IMongoCollection<Auction> collection, int currentPage, CancellationToken stoppingToken)
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
                logger.LogInformation("Starting cycle {Cycle}", page.LastUpdated);

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
                await collection.ReplaceOneAsync(filter, auction, new ReplaceOptions { IsUpsert = true }, stoppingToken);
            }

            // if this is the last page: stop
            if (currentPage == page.TotalPages - 1)
            {
                long now = (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalMilliseconds;
                int timeToWait = (int)(now - page.LastUpdated);
                return timeToWait;
            }

            // grab the next page
            return await ProcessActiveAuctions(client, collection, currentPage + 1, stoppingToken);
        }
    }
}
