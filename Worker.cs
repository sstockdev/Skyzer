using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using Newtonsoft.Json;
using SkyzerSync.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

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
            var collection = db.GetCollection<BsonDocument>("current_auctions");

            // Set up HttpClient
            using HttpClient client = new();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            while (!stoppingToken.IsCancellationRequested)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Processing Active Auctions"); // each cycle can be marked by auction.LastUpdated
                    await ProcessActiveAuctions(client, collection, Constants.ACTIVE_AUCTIONS_STARTING_PAGE, stoppingToken);
                    logger.LogInformation("Finished Processing Active Auctions");
                }
                return;
                //await Task.Delay(10000, stoppingToken);
            }
        }

        private async Task ProcessActiveAuctions(HttpClient client, IMongoCollection<BsonDocument> collection, int currentPage, CancellationToken stoppingToken)
        {
            var pageStr = await client.GetStringAsync(Constants.ACTIVE_AUCTIONS_URL + $"?page={currentPage}", stoppingToken);

            if (String.IsNullOrEmpty(pageStr))
            {
                logger.LogError("Hypixel API returned a null or empty string when grabbing active auctions");
                return;
            }

            var page = Newtonsoft.Json.JsonConvert.DeserializeObject<ActiveAuctionResponse>(pageStr);

            if (page == null)
            {
                logger.LogError("We were unable to deserialize the response from Hypixel API.");
                return;
            }

            if (!page.Success)
            {
                logger.LogWarning("Hypixel API returned no success.");
                return;
            }

            logger.LogInformation("Processing page {Page}", page.Page);

            // convert firstPage.LastUpdated to DateTime object
            // will use later
            // DateTime lastUpdated = DateTime.UnixEpoch.AddSeconds(page.LastUpdated);

            if (page.Auctions!.Count == 0)
            {
                logger.LogError("Hypixel API returned no auctions.");
                return;
            }

            // sync every auction on this page to mongo
            foreach (Auction auction in page.Auctions)
            {
                // you need to write to db
            }

            // if this is the last page; stop
            if (currentPage == page.TotalPages)
                return;

            // grab the next page
            await ProcessActiveAuctions(client, collection, currentPage + 1, stoppingToken);
        }
    }
}
