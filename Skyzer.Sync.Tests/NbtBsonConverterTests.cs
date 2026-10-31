using fNbt;
using MongoDB.Bson;
using Skyzer.Sync.Nbt;

namespace Skyzer.Sync.Tests
{
    public class NbtBsonConverterTests
    {
        [Fact]
        public void ToBson_MapsScalarTypes()
        {
            var c = new NbtCompound("")
            {
                new NbtByte("b", 5), new NbtShort("s", 300), new NbtInt("i", 70000),
                new NbtLong("l", 5_000_000_000L), new NbtFloat("f", 1.5f),
                new NbtDouble("d", 2.25), new NbtString("str", "x"),
            };

            var doc = NbtBsonConverter.ToBson(c).AsBsonDocument;

            Assert.Equal(BsonType.Int32, doc["b"].BsonType);
            Assert.Equal(5, doc["b"].AsInt32);
            Assert.Equal(300, doc["s"].AsInt32);
            Assert.Equal(70000, doc["i"].AsInt32);
            Assert.Equal(BsonType.Int64, doc["l"].BsonType);
            Assert.Equal(5_000_000_000L, doc["l"].AsInt64);
            Assert.Equal(1.5, doc["f"].AsDouble);
            Assert.Equal(2.25, doc["d"].AsDouble);
            Assert.Equal("x", doc["str"].AsString);
        }

        [Fact]
        public void ToBson_MapsListsArraysAndNestedCompounds()
        {
            var c = new NbtCompound("")
            {
                new NbtList("list", [new NbtString("a"), new NbtString("b")]),
                new NbtByteArray("bytes", [1, 2]),
                new NbtIntArray("ints", [7, 8]),
                new NbtLongArray("longs", [9L]),
                new NbtCompound("nested") { new NbtString("inner", "v") },
            };

            var doc = NbtBsonConverter.ToBson(c).AsBsonDocument;

            Assert.Equal(new BsonArray { "a", "b" }, doc["list"].AsBsonArray);
            Assert.Equal(new BsonArray { 1, 2 }, doc["bytes"].AsBsonArray);
            Assert.Equal(BsonType.Int32, doc["bytes"][0].BsonType);
            Assert.Equal(new BsonArray { 7, 8 }, doc["ints"].AsBsonArray);
            Assert.Equal(BsonType.Int64, doc["longs"][0].BsonType);
            Assert.Equal(9L, doc["longs"][0].AsInt64);
            Assert.Equal("v", doc["nested"]["inner"].AsString);
        }

        [Fact]
        public void ToBson_SanitizesKeys()
        {
            var c = new NbtCompound("") { new NbtInt("a.b", 1), new NbtInt("$x", 2) };

            var doc = NbtBsonConverter.ToBson(c).AsBsonDocument;

            Assert.Equal(1, doc["a_b"].AsInt32);
            Assert.Equal(2, doc["_x"].AsInt32);
            _ = doc.ToBson(); // serializes without throwing
        }
    }
}
