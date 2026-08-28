using System.Text.Json.Serialization;

namespace Skyzer.Sync.Models
{
    /// <summary>
    /// The model for the response returned from Hypixel API endpoint https://api.hypixel.net/v2/skyblock/auctions_ended
    /// </summary>
    public class EndedAuctionsResponse
    {
        /// <summary>
        /// Returns true if the request was successful.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// The last time the data provided was updated in milliseconds since unix epoch.
        /// </summary>
        [JsonPropertyName("lastUpdated")]
        public long LastUpdated { get; set; }

        /// <summary>
        /// The collection of auctions that have ended in the last 60 seconds.
        /// </summary>
        [JsonPropertyName("auctions")]
        public required List<EndedAuction> Auctions { get; set; }
    }
}