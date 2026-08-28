using MongoDB.Bson.Serialization.Attributes;

namespace Skyzer.Sync.Models
{
    /// <summary>
    /// The model of a cycle. A cycle is a sync run.
    /// </summary>
    public class Cycle
    {
        /// <summary>
        /// The ID of the cycle which is the last updated time of an active auctions API response.
        /// It is derived from the last updated time provided by the response from Hypixel API.
        /// It is in milliseconds since unix epoch.
        /// </summary>
        [BsonId]
        public long Id { get; set; }
    }
}
