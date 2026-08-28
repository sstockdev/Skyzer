using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace SkyzerSync.Models
{
    /// <summary>
    /// The model for an auction on Hypixel Skyblock
    /// </summary>
    public class Auction
    {
        /// <summary>
        /// The uuid of the auction.
        /// </summary>
        [BsonId]
        [JsonPropertyName("uuid")]
        public required string Uuid { get; set; }

        /// <summary>
        /// The uuid of the player who made the auction.
        /// </summary>
        [JsonPropertyName("auctioneer")]
        public required string Auctioneer { get; set; }

        /// <summary>
        /// The profile uuid of the player who made the auction.
        /// </summary>
        [JsonPropertyName("profile_id")]
        public string? ProfileId { get; set; }

        /// <summary>
        /// The list of player uuids in the auctioneer's co-op.
        /// If the player is not in a co-op, it contains just
        /// the auctioneer's uuid.
        /// </summary>
        [JsonPropertyName("coop")]
        public required List<string> Coop { get; set; }

        /// <summary>
        /// The UnixTime the auction started.
        /// </summary>
        [JsonPropertyName("start")]
        public long Start { get; set; }

        /// <summary>
        /// The UnixTime the auction will expire.
        /// </summary>
        [JsonPropertyName("end")]
        public long End { get; set; }

        /// <summary>
        /// The name of the item that is being auctioned.
        /// </summary>
        [JsonPropertyName("item_name")]
        public string? ItemName { get; set; }

        /// <summary>
        /// The lore description of the item that is being auctioned.
        /// </summary>
        [JsonPropertyName("item_lore")]
        public string? ItemLore { get; set; }

        /// <summary>
        /// Extra lore about the item?
        /// </summary>
        [JsonPropertyName("extra")]
        public required string Extra { get; set; }

        /// <summary>
        /// The list of categories the auctioned item falls in to.
        /// </summary>
        [JsonPropertyName("categories")]
        public required List<string> Categories { get; set; }

        /// <summary>
        /// The category of the auction item.
        /// </summary>
        [JsonPropertyName("category")]
        public required string Category { get; set; }

        /// <summary>
        /// The rarity tier of the item.
        /// </summary>
        [JsonPropertyName("tier")]
        public required string Tier { get; set; }

        /// <summary>
        /// The starting amount of coins the auctioneer has placed.
        /// </summary>
        [JsonPropertyName("starting_bid")]
        public long StartingBid { get; set; }

        /// <summary>
        /// The auction item's NBT data.
        /// </summary>
        [JsonPropertyName("item_bytes")]
        public string? ItemBytes { get; set; }

        /// <summary>
        /// Has the auction item been claimed already?
        /// </summary>
        [JsonPropertyName("claimed")]
        public bool Claimed { get; set; }

        /// <summary>
        /// If the auction item has already been claimed, who claimed it?
        /// Contains a list of player uuid's
        /// </summary>
        [JsonPropertyName("claimed_bidders")]
        public List<string>? ClaimedBidders { get; set; }

        /// <summary>
        /// The highest amount of coins that has been bid for the item.
        /// (and probably the current bid amount / how much it was bought for)
        /// </summary>
        [JsonPropertyName("highest_bid_amount")]
        public long HighestBidAmount { get; set; }

        /// <summary>
        /// The UnixTime of the last time the auction was updated.
        /// </summary>
        [JsonPropertyName("last_updated")]
        public long LastUpdated { get; set; }

        /// <summary>
        /// Is this auction a BIN (buy it now)?
        /// </summary>
        [JsonPropertyName("bin")]
        public bool Bin { get; set; }

        /// <summary>
        /// The list of bids players have put on the auction.
        /// </summary>
        [JsonPropertyName("bids")]
        public required List<Bid> Bids { get; set; }

        /// <summary>
        /// The uuid of the auction item.
        /// </summary>
        [JsonPropertyName("item_uuid")]
        public string? ItemUuid { get; set; }
    }
}
