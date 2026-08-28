using System.Text.Json.Serialization;

namespace Skyzer.Sync.Models
{
    /// <summary>
    /// The model for the auction object returned by the Hypixel API endpoint
    /// https://api.hypixel.net/v2/skyblock/auctions_ended
    /// </summary>
    public class EndedAuction
    {
        /// <summary>
        /// The unique UUID for the auction object.
        /// </summary>
        [JsonPropertyName("auction_id")]
        public string? AuctionId { get; set; }

        /// <summary>
        /// The UUID of the player who created the auction.
        /// </summary>
        [JsonPropertyName("seller")]
        public required string Seller { get; set; }

        /// <summary>
        /// The UUID of the profile the seller was playing on when they
        /// created the auction.
        /// </summary>
        [JsonPropertyName("seller_profile")]
        public string? SellerProfile { get; set; }

        /// <summary>
        /// The UUID of the player who claimed the item from the auction.
        /// </summary>
        [JsonPropertyName("buyer")]
        public string? Buyer { get; set; }

        /// <summary>
        /// The UUID of the profile the buyer was playing on when they
        /// claim the item from the auction.
        /// </summary>
        [JsonPropertyName("buyer_profile")]
        public string? BuyerProfile { get; set; }

        /// <summary>
        /// The time at which the auction ended in milliseconds since unix epoch.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public required long Timestamp { get; set; }

        /// <summary>
        /// The price in coins at which the buyer paid for the item in the auction.
        /// </summary>
        [JsonPropertyName("price")]
        public long Price { get; set; }

        /// <summary>
        /// Returns true if the auction was a Buy-It-Now auction or BIN.
        /// </summary>
        [JsonPropertyName("bin")]
        public bool Bin { get; set; }

        /// <summary>
        /// The NBT data of the auction's item.
        /// </summary>
        [JsonPropertyName("item_bytes")]
        public string? ItemBytes { get; set; }
    }
}
