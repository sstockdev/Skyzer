using SkyzerSync.Models;
using System.Text.Json.Serialization;

public class EndedAuctionsResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("lastUpdated")]
    public long LastUpdated { get; set; }

    [JsonPropertyName("auctions")]
    public required List<EndedAuction> Auctions { get; set; }
}