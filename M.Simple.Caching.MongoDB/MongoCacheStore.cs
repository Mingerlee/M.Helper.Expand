using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.IO;

namespace M.Simple.Caching.MongoDB;

internal sealed partial class MongoCacheStore : ISimpleCacheStore, ISimpleCacheAdminStore, ISimpleNativeCacheStore, IDisposable
{
    private readonly IMongoClient _client;
    private readonly IMongoCollection<BsonDocument> _collection;
    private readonly SemaphoreSlim _indexGate = new(1, 1);
    private bool _indexReady;
    private readonly bool _native;

    internal MongoCacheStore(SimpleMongoCacheOptions options, CacheDashboardScope scope = CacheDashboardScope.CurrentApplication)
    {
        var settings = MongoClientSettings.FromConnectionString(options.ConnectionString);
        settings.ServerSelectionTimeout = options.ServerSelectionTimeout;
        _client = new MongoClient(settings);
        _collection = _client.GetDatabase(options.DatabaseName).GetCollection<BsonDocument>(options.CollectionName);
        _native = scope is CacheDashboardScope.All or CacheDashboardScope.Prefixes;
    }

    private async Task EnsureIndex(CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _indexReady)) return;
        await _indexGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_indexReady) return;
            await _collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("expiresAt"),
                new CreateIndexOptions { Name = "M_Simple_Cache_ExpiresAt", ExpireAfter = TimeSpan.Zero }),
                cancellationToken: cancellationToken).ConfigureAwait(false);
            Volatile.Write(ref _indexReady, true);
        }
        finally { _indexGate.Release(); }
    }

    // TTL cleanup is delayed. Every read must independently exclude logically expired documents.
    private static FilterDefinition<BsonDocument> Live(string key) => new BsonDocument
    {
        ["_id"] = key,
        ["$expr"] = new BsonDocument("$or", new BsonArray
        {
            new BsonDocument("$eq", new BsonArray
            {
                new BsonDocument("$ifNull", new BsonArray { "$expiresAt", BsonNull.Value }), BsonNull.Value
            }),
            new BsonDocument("$gt", new BsonArray { "$expiresAt", "$$NOW" })
        })
    };

    public async Task<CacheStoreValue?> ReadAsync(string key, bool refreshSliding, CancellationToken cancellationToken)
    {
        await EnsureIndex(cancellationToken).ConfigureAwait(false);
        var filter = Live(key);
        var document = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (document is null) return null;
        if (refreshSliding && document.TryGetValue("slidingMilliseconds", out var sliding) && sliding.ToInt64() > 0)
        {
            var candidate = new BsonDocument("$add", new BsonArray { "$$NOW", "$slidingMilliseconds" });
            var refreshed = new BsonDocument("$cond", new BsonArray
            {
                new BsonDocument("$eq", new BsonArray
                {
                    new BsonDocument("$ifNull", new BsonArray { "$absoluteExpiresAt", BsonNull.Value }), BsonNull.Value
                }),
                candidate,
                new BsonDocument("$min", new BsonArray { candidate, "$absoluteExpiresAt" })
            });
            var update = new PipelineUpdateDefinition<BsonDocument>(new[]
            {
                new BsonDocument("$set", new BsonDocument("expiresAt", new BsonDocument("$cond", new BsonArray
                {
                    new BsonDocument("$gt", new BsonArray
                    {
                        new BsonDocument("$ifNull", new BsonArray { "$slidingMilliseconds", 0 }), 0
                    }),
                    refreshed,
                    new BsonDocument("$ifNull", new BsonArray { "$expiresAt", "$$REMOVE" })
                })))
            });
            document = await _collection.FindOneAndUpdateAsync(filter, update,
                new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After },
                cancellationToken).ConfigureAwait(false);
            if (document is null) return null;
        }
        return new(document["typeName"].AsString, document["payload"].AsByteArray);
    }

    public async Task WriteAsync(string key, CacheStoreValue value, CacheExpiration expiration, CancellationToken cancellationToken)
    {
        await EnsureIndex(cancellationToken).ConfigureAwait(false);
        var absolute = Milliseconds(expiration.AbsoluteExpiration);
        var sliding = Milliseconds(expiration.SlidingExpiration);
        var ttl = absolute == 0 ? sliding : sliding == 0 ? absolute : Math.Min(absolute, sliding);
        var fields = new BsonDocument
        {
            ["typeName"] = new BsonDocument("$literal", value.TypeName),
            ["payload"] = new BsonDocument("$literal", new BsonBinaryData(value.Payload)),
            ["absoluteExpiresAt"] = absolute > 0 ? new BsonDocument("$add", new BsonArray { "$$NOW", absolute }) : new BsonString("$$REMOVE"),
            ["slidingMilliseconds"] = sliding > 0 ? new BsonInt64(sliding) : new BsonString("$$REMOVE"),
            ["expiresAt"] = ttl > 0 ? new BsonDocument("$add", new BsonArray { "$$NOW", ttl }) : new BsonString("$$REMOVE")
        };
        var update = new PipelineUpdateDefinition<BsonDocument>(new[] { new BsonDocument("$set", fields) });
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await _collection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", key), update,
                    new UpdateOptions { IsUpsert = true }, cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey && attempt < 2)
            {
                // Concurrent first upserts may race on the unique _id index. Retry the atomic write.
            }
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
    {
        await EnsureIndex(cancellationToken).ConfigureAwait(false);
        return await _collection.Find(Live(key)).Project(Builders<BsonDocument>.Projection.Include("_id"))
            .Limit(1).AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        await EnsureIndex(cancellationToken).ConfigureAwait(false);
        await _collection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", key), cancellationToken).ConfigureAwait(false);
    }


    public string Backend => "MongoDB";

    private static FilterDefinition<BsonDocument> Managed(string prefix)
    {
        var filter = (BsonDocument)Live("").Render(new RenderArgs<BsonDocument>(
            BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry));
        filter.Remove("_id");
        filter["_id"] = new BsonDocument("$regex", "^" + System.Text.RegularExpressions.Regex.Escape(prefix));
        filter["typeName"] = new BsonDocument("$type", "string");
        filter["payload"] = new BsonDocument("$type", "binData");
        return filter;
    }

    public async Task<CacheScanPage> ScanAsync(string prefix, string? cursor, int pageSize, CancellationToken cancellationToken)
    {
        if (_native) return await ScanNativeAsync(prefix, cursor, pageSize, cancellationToken).ConfigureAwait(false);
        await EnsureIndex(cancellationToken).ConfigureAwait(false);
        var filter = Managed(prefix);
        if (cursor is not null) filter &= Builders<BsonDocument>.Filter.Gt("_id", cursor);
        var docs = await _collection.Find(filter).Sort(Builders<BsonDocument>.Sort.Ascending("_id"))
            .Limit(pageSize + 1).Project<BsonDocument>(new BsonDocument
            {
                ["_id"] = 1, ["typeName"] = 1, ["expiresAt"] = 1, ["absoluteExpiresAt"] = 1, ["slidingMilliseconds"] = 1,
                ["payloadSize"] = new BsonDocument("$binarySize", "$payload")
            }).ToListAsync(cancellationToken).ConfigureAwait(false);
        return new(docs.Take(pageSize).Select(Info).ToArray(),
            docs.Count > pageSize ? docs[pageSize - 1]["_id"].AsString : null);
    }

    public async Task<CacheEntryDetail?> PeekAsync(string key, CancellationToken cancellationToken)
    {
        if (_native) return await PeekNativeAsync(key, cancellationToken).ConfigureAwait(false);
        await EnsureIndex(cancellationToken).ConfigureAwait(false);
        var doc = await _collection.Find(Managed(key) & Builders<BsonDocument>.Filter.Eq("_id", key))
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return doc is null ? null : new(Info(doc), System.Text.Encoding.UTF8.GetString(doc["payload"].AsByteArray));
    }

    public async Task<bool> RemoveManagedAsync(string key, CancellationToken cancellationToken)
    {
        if (_native)
        {
            var removed = await _collection.DeleteOneAsync(NativeFilter("") & Builders<BsonDocument>.Filter.Eq("_id", DecodeId(key)), cancellationToken).ConfigureAwait(false);
            return removed.DeletedCount > 0;
        }
        await EnsureIndex(cancellationToken).ConfigureAwait(false);
        var deleted = await _collection.DeleteOneAsync(Managed(key) & Builders<BsonDocument>.Filter.Eq("_id", key), cancellationToken).ConfigureAwait(false);
        return deleted.DeletedCount > 0;
    }

    private static CacheEntryInfo Info(BsonDocument doc)
    {
        static DateTimeOffset? Date(BsonDocument doc, string name) =>
            doc.TryGetValue(name, out var value) && value.IsBsonDateTime ? new DateTimeOffset(value.ToUniversalTime()) : null;
        return new(EncodeId(doc["_id"]), doc["typeName"].AsString, doc.TryGetValue("payloadSize", out var size) ? size.ToInt64() : doc["payload"].AsByteArray.LongLength,
            Date(doc, "expiresAt"), Date(doc, "absoluteExpiresAt"),
            doc.TryGetValue("slidingMilliseconds", out var sliding) && sliding.ToInt64() > 0 ? TimeSpan.FromMilliseconds(sliding.ToInt64()) : null);
    }

    // Native MongoDB ids retain their BSON type; strings normally remain readable keys.
    private const string IdMarker = "~mongo:";
    private static string EncodeId(BsonValue id) => id.IsString && !string.IsNullOrWhiteSpace(id.AsString) && !id.AsString.StartsWith(IdMarker, StringComparison.Ordinal)
        ? id.AsString : IdMarker + Convert.ToBase64String(new BsonDocument("id", id).ToBson());
    private static BsonValue DecodeId(string key)
    {
        if (!key.StartsWith(IdMarker, StringComparison.Ordinal)) return new BsonString(key);
        try { return BsonSerializer.Deserialize<BsonDocument>(Convert.FromBase64String(key[IdMarker.Length..]))["id"]; }
        catch (Exception error) when (error is FormatException or System.IO.EndOfStreamException or ArgumentException)
        { throw new ArgumentException("Invalid MongoDB document identifier.", nameof(key)); }
    }

    private static FilterDefinition<BsonDocument> NativeFilter(string prefix)
    {
        var format = new BsonDocument { ["typeName"] = new BsonDocument("$type", "string"), ["payload"] = new BsonDocument("$type", "binData") };
        var expired = new BsonDocument("$and", new BsonArray
        {
            format,
            new BsonDocument("expiresAt", new BsonDocument("$type", "date")),
            new BsonDocument("$expr", new BsonDocument("$lte", new BsonArray { "$expiresAt", "$$NOW" }))
        });
        FilterDefinition<BsonDocument> filter = new BsonDocument("$nor", new BsonArray { expired });
        if (prefix.Length > 0)
            filter &= new BsonDocument("_id", new BsonDocument("$regex", "^" + System.Text.RegularExpressions.Regex.Escape(prefix)));
        return filter;
    }

    private static bool IsManaged(BsonDocument doc) => doc.TryGetValue("typeName", out var type) && type.IsString &&
        doc.TryGetValue("payload", out var payload) && payload.IsBsonBinaryData;

    private async Task<CacheScanPage> ScanNativeAsync(string prefix, string? cursor, int pageSize, CancellationToken ct)
    {
        var filter = NativeFilter(prefix);
        // $expr compares across BSON types; a normal query $gt is type-bracketed and would skip later id types.
        if (cursor is not null) filter &= new BsonDocument("$expr", new BsonDocument("$gt",
            new BsonArray { "$_id", new BsonDocument("$literal", DecodeId(cursor)) }));
        var docs = await _collection.Find(filter).Sort(Builders<BsonDocument>.Sort.Ascending("_id"))
            .Limit(pageSize + 1).Project<BsonDocument>(new BsonDocument
            {
                ["_id"] = 1, ["typeName"] = 1, ["expiresAt"] = 1, ["absoluteExpiresAt"] = 1, ["slidingMilliseconds"] = 1,
                ["bytes"] = new BsonDocument("$bsonSize", "$$ROOT"),
                ["managed"] = new BsonDocument("$and", new BsonArray
                {
                    new BsonDocument("$eq", new BsonArray { new BsonDocument("$type", "$typeName"), "string" }),
                    new BsonDocument("$eq", new BsonArray { new BsonDocument("$type", "$payload"), "binData" })
                }),
                ["payloadSize"] = new BsonDocument("$cond", new BsonArray
                {
                    new BsonDocument("$eq", new BsonArray { new BsonDocument("$type", "$payload"), "binData" }),
                    new BsonDocument("$binarySize", "$payload"), 0
                })
            }).ToListAsync(ct).ConfigureAwait(false);
        var entries = docs.Take(pageSize).Select(doc => doc["managed"].AsBoolean
            ? Info(doc) with { Key = EncodeId(doc["_id"]) }
            : new CacheEntryInfo(EncodeId(doc["_id"]), "MongoDB:document", doc["bytes"].ToInt64(), null, null, null) { StorageType = "document" }).ToArray();
        return new(entries, docs.Count > pageSize ? EncodeId(docs[pageSize - 1]["_id"]) : null);
    }

    private async Task<CacheEntryDetail?> PeekNativeAsync(string key, CancellationToken ct)
    {
        // Project a bounded preview on the server; never transfer a huge business document to the dashboard.
        var row = await _collection.Find(NativeFilter("") & Builders<BsonDocument>.Filter.Eq("_id", DecodeId(key)))
            .Project<BsonDocument>(new BsonDocument
            {
                ["_id"] = 1, ["bytes"] = new BsonDocument("$bsonSize", "$$ROOT"),
                ["document"] = new BsonDocument("$cond", new BsonArray
                {
                    new BsonDocument("$lte", new BsonArray { new BsonDocument("$bsonSize", "$$ROOT"), 65536 }),
                    "$$ROOT", BsonNull.Value
                })
            }).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (row is null) return null;
        var size = row["bytes"].ToInt64();
        var document = row["document"];
        if (document.IsBsonDocument && IsManaged(document.AsBsonDocument))
            return new(Info(document.AsBsonDocument) with { Key = key }, System.Text.Encoding.UTF8.GetString(document["payload"].AsByteArray));
        var json = document.IsBsonNull
            ? new BsonDocument { ["_id"] = row["_id"], ["previewOmitted"] = true, ["sizeBytes"] = size, ["limitBytes"] = 65536 }
            : document.AsBsonDocument;
        return new(new(key, "MongoDB:document", size, null, null, null) { StorageType = "document" },
            json.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson }));
    }

    private static long Milliseconds(TimeSpan? duration) => duration is { } value ? (long)Math.Ceiling(value.TotalMilliseconds) : 0;
    public void Dispose() => (_client as IDisposable)?.Dispose();
}
