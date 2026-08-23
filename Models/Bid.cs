using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkyzerSync.Models
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
        [JsonProperty("auction_id")]
        public string AuctionId { get; set; }
        /// <summary>
        /// The uuid of the bidder who placed the bid.
        /// </summary>
        [JsonProperty("bidder")]
        public string Bidder { get; set; }
        /// <summary>
        /// The profile uuid of the bidder who placed the bid.
        /// </summary>
        [JsonProperty("profile_id")]
        public string ProfileId { get; set; }
        /// <summary>
        /// The amount of coins the bidder placed on the bid.
        /// </summary>
        [JsonProperty("amount")]
        public int Amount { get; set; }
        /// <summary>
        /// The UnixTime the bidder placed the bid.
        /// </summary>
        [JsonProperty("timestamp")]
        public object Timestamp { get; set; }
    }
}
