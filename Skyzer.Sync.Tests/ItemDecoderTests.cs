using fNbt;
using Skyzer.Sync.Nbt;
using static Skyzer.Sync.Tests.ItemBytesBuilder;

namespace Skyzer.Sync.Tests
{
    public class ItemDecoderTests
    {
        [Fact]
        public void Decode_PlainItem()
        {
            var bytes = Encode(Item("ASPECT_OF_THE_END", display: d => d.Add(new NbtString("Name", "§9Aspect of the End"))));

            var item = ItemDecoder.Decode(bytes);

            Assert.Equal("ASPECT_OF_THE_END", item.SkyblockId);
            Assert.Equal("Aspect of the End", item.DisplayName);
            Assert.Equal(1, item.Count);
            Assert.Null(item.Reforge);
            Assert.Null(item.Enchantments);
            Assert.Null(item.Stars);
            Assert.Null(item.HotPotatoCount);
            Assert.Null(item.Recombobulated);
            Assert.Null(item.DyeColor);
            Assert.Null(item.Pet);
            Assert.Null(item.DecodeError);
            Assert.Equal(ItemDecoder.Version, item.DecoderVersion);
            Assert.NotNull(item.Nbt);
            Assert.Equal("ASPECT_OF_THE_END", item.Nbt["tag"]["ExtraAttributes"]["id"].AsString);
            Assert.Equal(0, item.Nbt["Damage"].AsInt32);
        }

        [Fact]
        public void Decode_UpgradedWeapon()
        {
            var bytes = Encode(Item("HYPERION", extra: e =>
            {
                e.Add(new NbtString("modifier", "heroic"));
                e.Add(new NbtCompound("enchantments") { new NbtInt("sharpness", 6), new NbtInt("ultimate_wise", 5) });
                e.Add(new NbtInt("upgrade_level", 5));
                e.Add(new NbtInt("hot_potato_count", 10));
                e.Add(new NbtInt("rarity_upgrades", 1));
            }));

            var item = ItemDecoder.Decode(bytes);

            Assert.Equal("heroic", item.Reforge);
            Assert.Equal(new Dictionary<string, int> { ["sharpness"] = 6, ["ultimate_wise"] = 5 }, item.Enchantments);
            Assert.Equal(5, item.Stars);
            Assert.Equal(10, item.HotPotatoCount);
            Assert.True(item.Recombobulated);
        }

        [Fact]
        public void Decode_StarsFallBackToDungeonItemLevel()
        {
            var bytes = Encode(Item("LIVID_DAGGER", extra: e => e.Add(new NbtInt("dungeon_item_level", 3))));

            Assert.Equal(3, ItemDecoder.Decode(bytes).Stars);
        }

        [Fact]
        public void Decode_RarityUpgradesZero_NotRecombobulated()
        {
            var bytes = Encode(Item("HYPERION", extra: e => e.Add(new NbtInt("rarity_upgrades", 0))));

            Assert.False(ItemDecoder.Decode(bytes).Recombobulated);
        }

        [Fact]
        public void Decode_DyedArmor()
        {
            var bytes = Encode(Item("SUPERIOR_DRAGON_CHESTPLATE", display: d => d.Add(new NbtInt("color", 1644825))));

            Assert.Equal(1644825, ItemDecoder.Decode(bytes).DyeColor);
        }

        [Fact]
        public void Decode_Stack()
        {
            var bytes = Encode(Item("ENCHANTED_DIAMOND", count: 64));

            Assert.Equal(64, ItemDecoder.Decode(bytes).Count);
        }

        [Fact]
        public void Decode_Pet()
        {
            const string petInfo = "{\"type\":\"ENDER_DRAGON\",\"tier\":\"LEGENDARY\",\"exp\":25353230.5,\"heldItem\":\"PET_ITEM_TIER_BOOST\",\"candyUsed\":0}";
            var bytes = Encode(Item("PET", extra: e => e.Add(new NbtString("petInfo", petInfo))));

            var item = ItemDecoder.Decode(bytes);

            Assert.NotNull(item.Pet);
            Assert.Equal("ENDER_DRAGON", item.Pet.Type);
            Assert.Equal("LEGENDARY", item.Pet.Tier);
            Assert.Equal(25353230.5, item.Pet.Exp);
            Assert.Equal("PET_ITEM_TIER_BOOST", item.Pet.HeldItem);
            Assert.Equal(0, item.Pet.CandyUsed);
            Assert.Equal(petInfo, item.Nbt!["tag"]["ExtraAttributes"]["petInfo"].AsString);
        }

        [Fact]
        public void Decode_PetWithBadPetInfo_PetIsNull()
        {
            var bytes = Encode(Item("PET", extra: e => e.Add(new NbtString("petInfo", "{not json"))));

            var item = ItemDecoder.Decode(bytes);

            Assert.Null(item.Pet);
            Assert.Null(item.DecodeError);
            Assert.Equal("PET", item.SkyblockId);
        }

        [Theory]
        [InlineData("5")]
        [InlineData("[1]")]
        [InlineData("null")]
        [InlineData("{\"type\":\"ENDER_DRAGON\",\"candyUsed\":1.5}")]
        [InlineData("{\"type\":\"ENDER_DRAGON\",\"candyUsed\":99999999999}")]
        public void Decode_PetWithUnexpectedPetInfoShape_KeepsRestOfItem(string petInfo)
        {
            var bytes = Encode(Item("PET", extra: e => e.Add(new NbtString("petInfo", petInfo))));

            var item = ItemDecoder.Decode(bytes);

            Assert.Null(item.DecodeError);
            Assert.Equal("PET", item.SkyblockId);
            Assert.NotNull(item.Nbt);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not base64!!")]
        [InlineData("aGVsbG8=")]
        public void Decode_Garbage_SetsErrorAndNeverThrows(string? input)
        {
            var item = ItemDecoder.Decode(input);

            Assert.False(string.IsNullOrEmpty(item.DecodeError));
            Assert.Null(item.SkyblockId);
            Assert.Null(item.Nbt);
            Assert.Equal(ItemDecoder.Version, item.DecoderVersion);
        }

        [Fact]
        public void Decode_MissingItemList_SetsError()
        {
            var noList = EncodeRoot(new NbtCompound("") { new NbtInt("other", 1) });
            var emptyList = EncodeRoot(new NbtCompound("") { new NbtList("i", NbtTagType.Compound) });

            Assert.False(string.IsNullOrEmpty(ItemDecoder.Decode(noList).DecodeError));
            Assert.False(string.IsNullOrEmpty(ItemDecoder.Decode(emptyList).DecodeError));
        }
    }
}
