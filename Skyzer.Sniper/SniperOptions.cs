namespace Skyzer.Sniper
{
    /// <summary>
    /// Configuration for the sniper worker, bound from the "Sniper" section of appsettings.
    /// </summary>
    public class SniperOptions
    {
        public const string SectionName = "Sniper";

        /// <summary>
        /// How often (in seconds) the detection cycle runs.
        /// </summary>
        public double CycleDelaySeconds { get; set; } = 60;

        /// <summary>
        /// The rolling window (in hours) of sold history used to build the market reference.
        /// </summary>
        public double MarketWindowHours { get; set; } = 24;

        /// <summary>
        /// The minimum number of sold samples required before an item key is priced.
        /// Guards against pricing off a single lucky sale.
        /// </summary>
        public int MinSampleCount { get; set; } = 5;

        /// <summary>
        /// A live listing is only flagged when its price is at or below
        /// (reference median * this fraction). 0.85 means at least 15% under market.
        /// </summary>
        public double MaxPriceFraction { get; set; } = 0.85;

        /// <summary>
        /// The auction-house sale tax fraction applied when estimating resale proceeds.
        /// </summary>
        public double AuctionHouseTax { get; set; } = 0.01;

        /// <summary>
        /// The minimum estimated profit (in coins) for a listing to be flagged.
        /// </summary>
        public long MinProfitCoins { get; set; } = 100000;
    }
}
