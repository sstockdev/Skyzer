using System.Text.Json.Serialization;

namespace Skyzer.Shared.Models
{
    /// <summary>
    /// The model that contains the information
    /// about a bid a player has placed on an auction.
    /// </summary>
    public class Bid
    {
        /// <summary>
        /// The uuid of the auction the bid was placed for.
        /// </summary>
        [JsonPropertyName("auction_id")]

        public string? AuctionId { get; set; }

        /// <summary>
        /// The uuid of the bidder who placed the bid.
        /// </summary>
        [JsonPropertyName("bidder")]
        public string? Bidder { get; set; }

        /// <summary>
        /// The profile uuid of the bidder who placed the bid.
        /// </summary>
        [JsonPropertyName("profile_id")]
        public string? ProfileId { get; set; }

        /// <summary>
        /// The amount of coins the bidder placed on the bid.
        /// </summary>
        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        /// <summary>
        /// The time at which the bid was created in milliseconds since unix epoch time.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }
    }
}
