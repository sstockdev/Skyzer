using System.Text.Json.Serialization;

namespace SkyzerSync.Models
{
    public class EndedAuction
    {
        [JsonPropertyName("auction_id")]
        public string? AuctionId { get; set; }

        [JsonPropertyName("seller")]
        public required string Seller { get; set; }

        [JsonPropertyName("seller_profile")]
        public string? SellerProfile { get; set; }

        [JsonPropertyName("buyer")]
        public string? Buyer { get; set; }

        [JsonPropertyName("buyer_profile")]
        public string? BuyerProfile { get; set; }

        [JsonPropertyName("timestamp")]
        public required long Timestamp { get; set; }

        [JsonPropertyName("price")]
        public long Price { get; set; }

        [JsonPropertyName("bin")]
        public bool Bin { get; set; }

        [JsonPropertyName("item_bytes")]
        public string? ItemBytes { get; set; }
    }
}
