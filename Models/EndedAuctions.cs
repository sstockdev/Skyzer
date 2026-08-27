using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkyzerSync.Models
{
    public class EndedAuction
    {
        [JsonProperty("auction_id")]
        public string? AuctionId { get; set; }

        [JsonProperty("seller")]
        public required string Seller { get; set; }

        [JsonProperty("seller_profile")]
        public string? SellerProfile { get; set; }

        [JsonProperty("buyer")]
        public string? Buyer { get; set; }

        [JsonProperty("buyer_profile")]
        public string? BuyerProfile { get; set; }

        [JsonProperty("timestamp")]
        public required long Timestamp { get; set; }

        [JsonProperty("price")]
        public long Price { get; set; }

        [JsonProperty("bin")]
        public bool Bin { get; set; }

        [JsonProperty("item_bytes")]
        public string? ItemBytes { get; set; }
    }
}
