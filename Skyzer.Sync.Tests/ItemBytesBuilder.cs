using fNbt;

namespace Skyzer.Sync.Tests
{
    /// <summary>
    /// Builds synthetic Hypixel <c>item_bytes</c> strings (base64 of gzipped NBT) for tests.
    /// </summary>
    public static class ItemBytesBuilder
    {
        /// <summary>
        /// Wraps <paramref name="item"/> as the single entry of the root <c>i</c> list,
        /// gzips it, and returns the base64 string.
        /// </summary>
        public static string Encode(NbtCompound item)
        {
            var root = new NbtCompound("") { new NbtList("i", [item]) };
            return EncodeRoot(root);
        }

        /// <summary>
        /// Gzips an arbitrary root compound and returns the base64 string.
        /// </summary>
        public static string EncodeRoot(NbtCompound root)
        {
            var bytes = new NbtFile(root).SaveToBuffer(NbtCompression.GZip);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Builds an item compound shaped like a Hypixel Skyblock item.
        /// </summary>
        public static NbtCompound Item(
            string skyblockId,
            Action<NbtCompound>? extra = null,
            Action<NbtCompound>? display = null,
            byte count = 1)
        {
            var extraAttributes = new NbtCompound("ExtraAttributes") { new NbtString("id", skyblockId) };
            extra?.Invoke(extraAttributes);

            var displayTag = new NbtCompound("display");
            display?.Invoke(displayTag);

            return new NbtCompound
            {
                new NbtShort("id", 276),
                new NbtByte("Count", count),
                new NbtShort("Damage", 0),
                new NbtCompound("tag") { displayTag, extraAttributes },
            };
        }
    }
}
