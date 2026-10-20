using MongoDB.Bson.Serialization.Attributes;

namespace Skyzer.Shared.Models
{
    /// <summary>
    /// A computed market-price reference for a single item key, derived from
    /// recently sold BIN auctions within a rolling window.
    /// </summary>
    public class MarketPrice
    {
        /// <summary>
        /// The item key this reference is for (e.g. "{ItemName}|{Tier}").
        /// </summary>
        [BsonId]
        public required string ItemKey { get; set; }

        /// <summary>
        /// The item name component of the key.
        /// </summary>
        public string? ItemName { get; set; }

        /// <summary>
        /// The rarity tier component of the key.
        /// </summary>
        public string? Tier { get; set; }

        /// <summary>
        /// The median sold price over the window.
        /// </summary>
        public long Median { get; set; }

        /// <summary>
        /// The 25th-percentile sold price over the window.
        /// </summary>
        public long LowerPercentile { get; set; }

        /// <summary>
        /// The number of sold samples the stats were computed from.
        /// </summary>
        public int SampleCount { get; set; }

        /// <summary>
        /// The UnixTime (ms) when this reference was computed.
        /// </summary>
        public long ComputedAt { get; set; }
    }
}
