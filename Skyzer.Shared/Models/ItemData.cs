using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Skyzer.Shared.Models
{
    /// <summary>
    /// The decoded contents of an auction's <c>item_bytes</c> NBT. Computed by Skyzer.Sync,
    /// not provided by Hypixel API.
    /// </summary>
    public class ItemData
    {
        /// <summary>
        /// The Skyblock item id from <c>ExtraAttributes.id</c> (e.g. "HYPERION", "PET").
        /// </summary>
        [BsonIgnoreIfNull]
        public string? SkyblockId { get; set; }

        /// <summary>
        /// The item's display name with Minecraft colour codes stripped.
        /// </summary>
        [BsonIgnoreIfNull]
        public string? DisplayName { get; set; }

        /// <summary>
        /// The stack size of the item.
        /// </summary>
        [BsonIgnoreIfNull]
        public int? Count { get; set; }

        /// <summary>
        /// The reforge applied to the item, from <c>ExtraAttributes.modifier</c>.
        /// </summary>
        [BsonIgnoreIfNull]
        public string? Reforge { get; set; }

        /// <summary>
        /// The item's enchantments, keyed by enchantment name with the level as value.
        /// </summary>
        [BsonIgnoreIfNull]
        public Dictionary<string, int>? Enchantments { get; set; }

        /// <summary>
        /// The number of stars on the item, from <c>upgrade_level</c> or <c>dungeon_item_level</c>.
        /// </summary>
        [BsonIgnoreIfNull]
        public int? Stars { get; set; }

        /// <summary>
        /// The number of hot potato books applied to the item.
        /// </summary>
        [BsonIgnoreIfNull]
        public int? HotPotatoCount { get; set; }

        /// <summary>
        /// Whether the item has been recombobulated. Null when the item has no <c>rarity_upgrades</c> tag.
        /// </summary>
        [BsonIgnoreIfNull]
        public bool? Recombobulated { get; set; }

        /// <summary>
        /// The item's dye colour as an RGB integer, from <c>display.color</c>.
        /// </summary>
        [BsonIgnoreIfNull]
        public int? DyeColor { get; set; }

        /// <summary>
        /// The pet's details when the item is a pet.
        /// </summary>
        [BsonIgnoreIfNull]
        public PetData? Pet { get; set; }

        /// <summary>
        /// The entire decoded item compound, unfiltered.
        /// </summary>
        [BsonIgnoreIfNull]
        public BsonDocument? Nbt { get; set; }

        /// <summary>
        /// The version of the decoder that produced this data.
        /// </summary>
        public int DecoderVersion { get; set; }

        /// <summary>
        /// Why decoding failed, or null if it succeeded.
        /// </summary>
        [BsonIgnoreIfNull]
        public string? DecodeError { get; set; }
    }

    /// <summary>
    /// The details of a pet, parsed from the <c>petInfo</c> JSON inside the item's NBT.
    /// </summary>
    public class PetData
    {
        /// <summary>
        /// The pet type (e.g. "ENDER_DRAGON").
        /// </summary>
        [BsonIgnoreIfNull]
        public string? Type { get; set; }

        /// <summary>
        /// The pet's rarity tier.
        /// </summary>
        [BsonIgnoreIfNull]
        public string? Tier { get; set; }

        /// <summary>
        /// The pet's experience.
        /// </summary>
        [BsonIgnoreIfNull]
        public double? Exp { get; set; }

        /// <summary>
        /// The pet item the pet is holding.
        /// </summary>
        [BsonIgnoreIfNull]
        public string? HeldItem { get; set; }

        /// <summary>
        /// The number of pet candies used on the pet.
        /// </summary>
        [BsonIgnoreIfNull]
        public int? CandyUsed { get; set; }
    }
}
