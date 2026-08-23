using Newtonsoft.Json;

namespace SkyzerSync.Models
{
    /// <summary>
    /// The model for the response given by Hypixel API. https://api.hypixel.net/#tag/SkyBlock/paths/~1v2~1skyblock~1auctions/get
    /// </summary>
    public class ActiveAuctionResponse
    {
        /// <summary>
        /// Whether or not the request was a success.
        /// </summary>
        [JsonProperty("success")]
        public bool Success { get; set; }
        /// <summary>
        /// The current page number in the collection of pages.
        /// </summary>
        [JsonProperty("page")]
        public int Page { get; set; }
        /// <summary>
        /// The total count of pages available to grab.
        /// </summary>
        [JsonProperty("totalPages")]
        public int TotalPages { get; set; }
        /// <summary>
        /// The total number of current auctions available to grab.
        /// </summary>
        [JsonProperty("totalAuctions")]
        public int TotalAuctions { get; set; }
        /// <summary>
        /// The last time this endpoint was updated.
        /// </summary>
        [JsonProperty("lastUpdated")]
        public long LastUpdated { get; set; }
        /// <summary>
        /// The list of current auctions.
        /// </summary>
        [JsonProperty("auctions")]
        public List<Auction> Auctions { get; set; }
    }
}
