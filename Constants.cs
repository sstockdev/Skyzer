namespace SkyzerSync
{
    public static class Constants
    {
        /// <summary>
        /// The active auctions API endpoint from Hypixel API.
        /// </summary>
        public const string ACTIVE_AUCTIONS_URL = "https://api.hypixel.net/v2/skyblock/auctions";

        public const int ACTIVE_AUCTIONS_DELAY = 30 * 1000;

        /// <summary>
        /// The starting page of the auctions API endpoint.
        /// </summary>
        public const int ACTIVE_AUCTIONS_STARTING_PAGE = 0;

        /// <summary>
        /// The ended auctions API endpoint from Hypixel API.
        /// </summary>
        public const string ENDED_AUCTIONS_URL = "https://api.hypixel.net/v2/skyblock/auctions_ended";\

        public const int ENDED_AUCTIONS_DELAY = 60 * 1000;
    }
}
