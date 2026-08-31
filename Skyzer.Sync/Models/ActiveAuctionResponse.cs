using System.Text.Json.Serialization;

namespace Skyzer.Sync.Models
{
    /// <summary>
    /// The model for the response given by Hypixel API. https://api.hypixel.net/v2/skyblock/auctions
    /// </summary>
    public class ActiveAuctionResponse
    {
        /// <summary>
        /// Whether or not the request was a success.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        /// <summary>
        /// The current page number in the collection of pages.
        /// </summary>
        [JsonPropertyName("page")]
        public int Page { get; set; }
        /// <summary>
        /// The total count of pages available to grab.
        /// </summary>
        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }
        /// <summary>
        /// The total number of current auctions available to grab.
        /// </summary>
        [JsonPropertyName("totalAuctions")]
        public int TotalAuctions { get; set; }
        /// <summary>
        /// The last time this endpoint was updated.
        /// </summary>
        [JsonPropertyName("lastUpdated")]
        public long LastUpdated { get; set; }
        /// <summary>
        /// The list of current auctions.
        /// </summary>
        [JsonPropertyName("auctions")]
        public List<Auction>? Auctions { get; set; }
    }
}
