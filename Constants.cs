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
        /// The delay (in milliseconds) to add to the task delay between cycles (to prevent grabbing the same cycle twice in a row)
        /// </summary>
        public const int ACTIVE_AUCTIONS_TASK_DELAY = 20;
    }
}
