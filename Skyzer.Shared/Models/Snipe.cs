using MongoDB.Bson.Serialization.Attributes;

namespace Skyzer.Shared.Models
{
    /// <summary>
    /// A detected flip opportunity: a live BIN auction listed below its market reference.
    /// </summary>
    public class Snipe
    {
        /// <summary>
        /// The uuid of the auction this opportunity refers to.
        /// </summary>
        [BsonId]
        public required string Uuid { get; set; }

        /// <summary>
        /// The item key used to price the opportunity.
        /// </summary>
        public required string ItemKey { get; set; }

        /// <summary>
        /// The item name.
        /// </summary>
        public string? ItemName { get; set; }

        /// <summary>
        /// The rarity tier.
        /// </summary>
        public string? Tier { get; set; }

        /// <summary>
        /// The listed BIN price.
        /// </summary>
        public long ListedPrice { get; set; }

        /// <summary>
        /// The market reference price (median) the opportunity was priced against.
        /// </summary>
        public long ReferencePrice { get; set; }

        /// <summary>
        /// The estimated profit after auction-house tax (coins).
        /// </summary>
        public long EstimatedProfit { get; set; }

        /// <summary>
        /// The number of sold samples backing the reference.
        /// </summary>
        public int SampleCount { get; set; }

        /// <summary>
        /// The UnixTime (ms) the listed auction expires.
        /// </summary>
        public long End { get; set; }

        /// <summary>
        /// The UnixTime (ms) when this opportunity was detected.
        /// </summary>
        public long DetectedAt { get; set; }
    }
}
