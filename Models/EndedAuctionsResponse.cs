using Newtonsoft.Json;
using SkyzerSync.Models;

public class EndedAuctionsResponse
{
    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("lastUpdated")]
    public long LastUpdated { get; set; }

    [JsonProperty("auctions")]
    public required List<EndedAuction> Auctions { get; set; }
}