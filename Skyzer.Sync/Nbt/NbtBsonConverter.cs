using fNbt;
using MongoDB.Bson;

namespace Skyzer.Sync.Nbt
{
    /// <summary>
    /// Converts fNbt tags into their BSON equivalents so decoded items can be stored in MongoDB.
    /// </summary>
    internal static class NbtBsonConverter
    {
        /// <summary>
        /// Converts an NBT tag, and everything nested inside it, to a BSON value.
        /// </summary>
        /// <param name="tag">The NBT tag to convert.</param>
        /// <returns>The equivalent BSON value.</returns>
        internal static BsonValue ToBson(NbtTag tag)
        {
            return tag switch
            {
                NbtCompound compound => new BsonDocument(compound.Select(child =>
                    new BsonElement(SanitizeKey(child.Name ?? ""), ToBson(child)))),
                NbtList list => new BsonArray(list.Select(ToBson)),
                NbtByte b => new BsonInt32(b.Value),
                NbtShort s => new BsonInt32(s.Value),
                NbtInt i => new BsonInt32(i.Value),
                NbtLong l => new BsonInt64(l.Value),
                NbtFloat f => new BsonDouble(f.Value),
                NbtDouble d => new BsonDouble(d.Value),
                NbtString str => new BsonString(str.Value),
                NbtByteArray bytes => new BsonArray(bytes.Value.Select(v => (int)v)),
                NbtIntArray ints => new BsonArray(ints.Value),
                NbtLongArray longs => new BsonArray(longs.Value),
                _ => BsonNull.Value,
            };
        }

        /// <summary>
        /// Makes an NBT key safe to use as a MongoDB field name by replacing every '.'
        /// and a leading '$' with '_'.
        /// </summary>
        /// <param name="key">The NBT key.</param>
        /// <returns>The sanitized key.</returns>
        internal static string SanitizeKey(string key)
        {
            var sanitized = key.Replace('.', '_');
            return sanitized.StartsWith('$') ? "_" + sanitized[1..] : sanitized;
        }
    }
}
