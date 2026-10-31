using fNbt;
using Skyzer.Shared.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Skyzer.Sync.Nbt
{
    /// <summary>
    /// Decodes Hypixel's <c>item_bytes</c> (base64 of gzipped NBT) into <see cref="ItemData"/>.
    /// </summary>
    public static partial class ItemDecoder
    {
        /// <summary>
        /// The current decoder version. Bump this whenever decoding logic changes so
        /// stale records can be found and re-decoded.
        /// </summary>
        public const int Version = 1;

        /// <summary>
        /// Decodes an auction's item bytes. Never throws: on failure the returned
        /// <see cref="ItemData.DecodeError"/> says why.
        /// </summary>
        /// <param name="itemBytes">The base64 encoded, gzipped NBT from Hypixel API.</param>
        /// <returns>The decoded item data.</returns>
        public static ItemData Decode(string? itemBytes)
        {
            if (string.IsNullOrEmpty(itemBytes))
                return new ItemData { DecoderVersion = Version, DecodeError = "item_bytes was empty" };

            try
            {
                var buffer = Convert.FromBase64String(itemBytes);
                var file = new NbtFile();
                file.LoadFromBuffer(buffer, 0, buffer.Length, NbtCompression.AutoDetect);

                if (file.RootTag.Get("i") is not NbtList list || list.Count == 0 || list[0] is not NbtCompound itemTag)
                    return new ItemData { DecoderVersion = Version, DecodeError = "item NBT has no item in its 'i' list" };

                return FromItemTag(itemTag);
            }
            catch (Exception ex)
            {
                return new ItemData { DecoderVersion = Version, DecodeError = ex.Message };
            }
        }

        private static ItemData FromItemTag(NbtCompound itemTag)
        {
            var tag = itemTag.Get("tag") as NbtCompound;
            var display = tag?.Get("display") as NbtCompound;
            var extra = tag?.Get("ExtraAttributes") as NbtCompound;

            var skyblockId = extra?.Get("id")?.StringValue;
            var rarityUpgrades = ReadInt(extra?.Get("rarity_upgrades"));

            return new ItemData
            {
                SkyblockId = skyblockId,
                DisplayName = display?.Get("Name") is NbtString name ? ColorCode().Replace(name.Value, "") : null,
                Count = ReadInt(itemTag.Get("Count")),
                Reforge = (extra?.Get("modifier") as NbtString)?.Value,
                Enchantments = ReadEnchantments(extra?.Get("enchantments") as NbtCompound),
                Stars = ReadInt(extra?.Get("upgrade_level")) ?? ReadInt(extra?.Get("dungeon_item_level")),
                HotPotatoCount = ReadInt(extra?.Get("hot_potato_count")),
                Recombobulated = rarityUpgrades.HasValue ? rarityUpgrades > 0 : null,
                DyeColor = ReadInt(display?.Get("color")),
                Pet = skyblockId == "PET" ? ReadPet((extra?.Get("petInfo") as NbtString)?.Value) : null,
                Nbt = NbtBsonConverter.ToBson(itemTag).AsBsonDocument,
                DecoderVersion = Version,
            };
        }

        /// <summary>
        /// Reads an integral NBT value regardless of its stored width.
        /// </summary>
        private static int? ReadInt(NbtTag? tag) => tag switch
        {
            NbtByte b => b.Value,
            NbtShort s => s.Value,
            NbtInt i => i.Value,
            NbtLong l => (int)l.Value,
            _ => null,
        };

        private static Dictionary<string, int>? ReadEnchantments(NbtCompound? enchantments)
        {
            if (enchantments == null)
                return null;

            var result = new Dictionary<string, int>();
            foreach (var enchantment in enchantments)
            {
                if (enchantment.Name != null && ReadInt(enchantment) is int level)
                    result[enchantment.Name] = level;
            }
            return result;
        }

        /// <summary>
        /// Parses the <c>petInfo</c> JSON string. Returns null if it is missing or malformed,
        /// without failing the rest of the decode.
        /// </summary>
        private static PetData? ReadPet(string? petInfo)
        {
            if (string.IsNullOrEmpty(petInfo))
                return null;

            try
            {
                using var json = JsonDocument.Parse(petInfo);
                var root = json.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                    return null;

                return new PetData
                {
                    Type = ReadString(root, "type"),
                    Tier = ReadString(root, "tier"),
                    Exp = root.TryGetProperty("exp", out var exp) && exp.ValueKind == JsonValueKind.Number && exp.TryGetDouble(out var expValue) ? expValue : null,
                    HeldItem = ReadString(root, "heldItem"),
                    CandyUsed = root.TryGetProperty("candyUsed", out var candy) && candy.ValueKind == JsonValueKind.Number && candy.TryGetInt32(out var candyValue) ? candyValue : null,
                };
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string? ReadString(JsonElement element, string property) =>
            element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

        [GeneratedRegex("§.")]
        private static partial Regex ColorCode();
    }
}
