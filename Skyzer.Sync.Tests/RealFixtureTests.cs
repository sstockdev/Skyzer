using System.Text.Json;
using Skyzer.Shared.Models;
using Skyzer.Sync.Nbt;

namespace Skyzer.Sync.Tests
{
    /// <summary>
    /// Decodes item_bytes captured from the live Hypixel API, pinning the real-world format.
    /// </summary>
    public class RealFixtureTests
    {
        private record Fixture(string category, string item_bytes);

        private static readonly Dictionary<string, string> Fixtures = JsonSerializer
            .Deserialize<List<Fixture>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "real_items.json")))!
            .ToDictionary(f => f.category, f => f.item_bytes);

        public static TheoryData<string> Categories() =>
            ["plain", "enchanted_reforged_starred", "recombobulated", "pet", "stack", "dyed_armor"];

        [Theory]
        [MemberData(nameof(Categories))]
        public void Decode_RealFixture_Succeeds(string category)
        {
            var item = ItemDecoder.Decode(Fixtures[category]);

            Assert.Null(item.DecodeError);
            Assert.NotNull(item.SkyblockId);
            Assert.NotNull(item.Nbt);
            Assert.True(MatchesCategory(category, item), $"fixture '{category}' did not decode as its category");
        }

        private static bool MatchesCategory(string category, ItemData d) => category switch
        {
            "pet" => d.SkyblockId == "PET" && d.Pet?.Type != null,
            "enchanted_reforged_starred" => d.Enchantments?.Count > 0 && d.Reforge != null && d.Stars > 0,
            "recombobulated" => d.Recombobulated == true,
            "stack" => d.Count > 1,
            "dyed_armor" => d.DyeColor != null,
            "plain" => d.Reforge == null && d.Enchantments == null && d.Stars == null && d.HotPotatoCount == null
                && d.Recombobulated == null && d.DyeColor == null && d.Pet == null && d.Count == 1,
            _ => false,
        };
    }
}
