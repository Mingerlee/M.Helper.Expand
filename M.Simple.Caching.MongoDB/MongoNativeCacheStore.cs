using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace M.Simple.Caching.MongoDB;

internal sealed partial class MongoCacheStore
{
    public async Task<CacheResult<T>> ReadNativeAsync<T>(string key, SimpleNativeCacheOptions options, CancellationToken cancellationToken)
    {
        // Native string keys match BSON string ids literally. They are never implicitly converted to ObjectId.
        var row = await _collection.Find(Builders<BsonDocument>.Filter.Eq("_id", new BsonString(key)))
            .Project<BsonDocument>(new BsonDocument
            {
                ["size"] = new BsonDocument("$bsonSize", "$$ROOT"),
                ["document"] = new BsonDocument("$cond", new BsonArray
                {
                    new BsonDocument("$lte", new BsonArray { new BsonDocument("$bsonSize", "$$ROOT"), options.MaxPayloadBytes }),
                    "$$ROOT", BsonNull.Value
                })
            }).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (row is null) return new(false, default);
        if (row["size"].ToInt64() > options.MaxPayloadBytes)
            throw new InvalidOperationException("Native BSON document exceeds MaxPayloadBytes; no partial data was returned.");
        var document = row["document"].AsBsonDocument;
        if (typeof(T) == typeof(BsonDocument)) return new(true, (T)(object)document);
        if (typeof(T) == typeof(byte[])) return new(true, (T)(object)document.ToBson());
        return new(true, BsonSerializer.Deserialize<T>(document));
    }

    public async Task<bool> NativeExistsAsync(string key, CancellationToken cancellationToken)
    {
        var found = await _collection.Find(Builders<BsonDocument>.Filter.Eq("_id", new BsonString(key)))
            .Project<BsonDocument>(new BsonDocument("_id", 1)).Limit(1)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return found is not null;
    }
}
