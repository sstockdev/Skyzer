using MongoDB.Bson.Serialization.Attributes;

namespace SkyzerSync.Models
{
    public class Cycle
    {
        /// <summary>
        /// The id of the cycle which is the last updated time of an active auctions API response.
        /// </summary>
        [BsonId]
        public long Id { get; set; }
    }
}
