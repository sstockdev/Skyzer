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
        /// The delay between updates on the API endpoints in seconds.
        /// </summary>
        public const double API_ENDPOINT_DELAY = 61;

        /// <summary>
        /// The ended auctions API endpoint from Hypixel API.
        /// </summary>
        public const string ENDED_AUCTIONS_URL = "https://api.hypixel.net/v2/skyblock/auctions_ended";
    }
}
