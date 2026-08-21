using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkyzerSync.Models
{
    /// <summary>
    /// The model for an auction on Hypixel Skyblock
    /// </summary>
    class Auction
    {
        /// <summary>
        /// The uuid of the auction.
        /// </summary>
        [BsonId]
        [BsonIgnoreIfDefault]
        [JsonProperty("uuid")]
        public string Uuid { get; set; }
        /// <summary>
        /// The uuid of the player who made the auction.
        /// </summary>
        [JsonProperty("auctioneer")]
        public string Auctioneer { get; set; }
        /// <summary>
        /// The profile uuid of the player who made the auction.
        /// </summary>
        [JsonProperty("profile_id")]
        public string ProfileId { get; set; }
        /// <summary>
        /// The list of player uuids in the auctioneer's co-op.
        /// If the player is not in a co-op, it contains just
        /// the auctioneer's uuid.
        /// </summary>
        [JsonProperty("coop")]
        public List<string> Coop { get; set; }
        /// <summary>
        /// The UnixTime the auction started.
        /// </summary>
        [JsonProperty("start")]
        public object Start { get; set; }
        /// <summary>
        /// The UnixTime the auction will expire.
        /// </summary>
        [JsonProperty("end")]
        public object End { get; set; }
        /// <summary>
        /// The name of the item that is being auctioned.
        /// </summary>
        [JsonProperty("item_name")]
        public string ItemName { get; set; }
        /// <summary>
        /// The lore description of the item that is being auctioned.
        /// </summary>
        [JsonProperty("item_lore")]
        public string ItemLore { get; set; }
        /// <summary>
        /// Extra lore about the item?
        /// </summary>
        [JsonProperty("extra")]
        public string Extra { get; set; }
        /// <summary>
        /// The list of categories the auctioned item falls in to.
        /// </summary>
        [JsonProperty("categories")]
        public List<string> Categories { get; set; }

        /// <summary>
        /// The category of the auction item.
        /// </summary>
        [JsonProperty("category")]
        public string Category { get; set; }

        /// <summary>
        /// The rarity tier of the item.
        /// </summary>
        [JsonProperty("tier")]
        public string Tier { get; set; }
        /// <summary>
        /// The starting amount of coins the auctioneer has placed.
        /// </summary>
        [JsonProperty("starting_bid")]
        public int StartingBid { get; set; }
        /// <summary>
        /// The auction item's NBT data.
        /// </summary>
        [JsonProperty("item_bytes")]
        public string ItemBytes { get; set; }
        /// <summary>
        /// Has the auction item been claimed already?
        /// </summary>
        [JsonProperty("claimed")]
        public bool Claimed { get; set; }
        /// <summary>
        /// If the auction item has already been claimed, who claimed it?
        /// Contains a list of player uuid's
        /// </summary>
        [JsonProperty("claimed_bidders")]
        public List<object> ClaimedBidders { get; set; }
        /// <summary>
        /// The highest amount of coins that has been bid for the item.
        /// (and probably the current bid amount / how much it was bought for)
        /// </summary>
        [JsonProperty("highest_bid_amount")]
        public int HighestBidAmount { get; set; }
        /// <summary>
        /// The UnixTime of the last time the auction was updated.
        /// </summary>
        [JsonProperty("last_updated")]
        public object LastUpdated { get; set; }
        /// <summary>
        /// Is this auction a BIN (buy it now)?
        /// </summary>
        [JsonProperty("bin")]
        public bool Bin { get; set; }
        /// <summary>
        /// The list of bids players have put on the auction.
        /// </summary>
        [JsonProperty("bids")]
        public List<Bid> Bids { get; set; }

        /// <summary>
        /// The uuid of the auction item.
        /// </summary>
        [JsonProperty("item_uuid")]
        public string ItemUuid { get; set; }
    }
}
