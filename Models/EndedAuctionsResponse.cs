using Newtonsoft.Json;

public class EndedAuctionsResponse
{
    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("lastUpdated")]
    public long LastUpdated { get; set; }

    [JsonProperty("auctions")]
    public required List<EndedAuction> Auctions { get; set; }
}

public class EndedAuction
{
    [JsonProperty("auction_id")]
    public required string AuctionId { get; set; }

    [JsonProperty("seller")]
    public required string Seller { get; set; }

    [JsonProperty("seller_profile")]
    public required string SellerProfile { get; set; }

    [JsonProperty("buyer")]
    public required string Buyer { get; set; }

    [JsonProperty("buyer_profile")]
    public required string BuyerProfile { get; set; }

    [JsonProperty("timestamp")]
    public required long Timestamp { get; set; }

    [JsonProperty("price")]
    public long Price { get; set; }

    [JsonProperty("bin")]
    public bool Bin { get; set; }

    [JsonProperty("item_bytes")]
    public required string ItemBytes { get; set; }
}