using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Skyzer.Shared.Models;
using static Skyzer.Sync.Tests.ItemBytesBuilder;

namespace Skyzer.Sync.Tests
{
    public class ActiveAuctionsUpsertTests
    {
        private static Auction NewAuction(string? itemBytes) => new()
        {
            Uuid = "auction-1",
            Auctioneer = "seller",
            Coop = ["seller"],
            Extra = "",
            Categories = [],
            Category = "weapon",
            Tier = "LEGENDARY",
            Bids = [],
            Bin = true,
            ItemBytes = itemBytes,
        };

        private static BsonDocument Render(UpdateOneModel<Auction> model) =>
            model.Update.Render(new RenderArgs<Auction>(
                BsonSerializer.LookupSerializer<Auction>(), BsonSerializer.SerializerRegistry)).AsBsonDocument;

        [Fact]
        public void BuildUpsert_ItemFieldsOnlyOnInsert()
        {
            var bytes = Encode(Item("HYPERION"));

            var update = Render(ActiveAuctionsWorker.BuildUpsert(NewAuction(bytes), 123, isNew: true));

            var set = update["$set"].AsBsonDocument;
            Assert.False(set.Contains("ItemBytes"));
            Assert.False(set.Contains("Item"));
            Assert.False(set.Contains("FirstSeen"));
            Assert.Equal(123, set["LastSeen"].ToInt64());

            var setOnInsert = update["$setOnInsert"].AsBsonDocument;
            Assert.Equal(123, setOnInsert["FirstSeen"].ToInt64());
            Assert.Equal(bytes, setOnInsert["ItemBytes"].AsString);
            Assert.Equal("HYPERION", setOnInsert["Item"]["SkyblockId"].AsString);
        }

        [Fact]
        public void BuildUpsert_DecodeFailure_StillUpserts()
        {
            var model = ActiveAuctionsWorker.BuildUpsert(NewAuction("garbage"), 123, isNew: true);
            var setOnInsert = Render(model)["$setOnInsert"].AsBsonDocument;

            Assert.True(model.IsUpsert);
            Assert.True(setOnInsert["Item"].AsBsonDocument.Contains("DecodeError"));
            Assert.Equal("garbage", setOnInsert["ItemBytes"].AsString);
        }

        [Fact]
        public void BuildUpsert_ExistingAuction_SendsNoItemPayload()
        {
            var auction = NewAuction(Encode(Item("HYPERION")));

            var update = Render(ActiveAuctionsWorker.BuildUpsert(auction, 123, isNew: false));

            var set = update["$set"].AsBsonDocument;
            Assert.False(set.Contains("ItemBytes"));
            Assert.False(set.Contains("Item"));
            Assert.Equal(123, set["LastSeen"].ToInt64());

            var setOnInsert = update["$setOnInsert"].AsBsonDocument;
            Assert.Equal(123, setOnInsert["FirstSeen"].ToInt64());
            Assert.False(setOnInsert.Contains("ItemBytes"));
            Assert.False(setOnInsert.Contains("Item"));
            Assert.Null(auction.Item); // not decoded at all
        }
    }
}
