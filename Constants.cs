namespace SkyzerSync
{
    public static class Constants
    {
        /// <summary>
        /// The active auctions API endpoint from Hypixel API.
        /// </summary>
        public const string ACTIVE_AUCTIONS_URL = "https://api.hypixel.net/v2/skyblock/auctions";

        /// <summary>
        /// The starting page of the auctions API endpoint.
        /// </summary>
        public const int ACTIVE_AUCTIONS_STARTING_PAGE = 0;

        /// <summary>
        /// The ended auctions API endpoint from Hypixel API.
        /// </summary>
        public const string ENDED_AUCTIONS_URL = "https://api.hypixel.net/v2/skyblock/auctions_ended";

        /// <summary>
        /// The standard delay between api endpoint updates in milliseconds.
        /// </summary>
        public const int API_DELAY = 60000;
    }
}
