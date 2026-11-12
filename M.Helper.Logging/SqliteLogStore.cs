using System.Text;
using Microsoft.Data.Sqlite;

namespace M.Helper.Logging;

/// <summary>本地日志查询服务，每次查询独立连接，不与写入器共享连接。</summary>
public sealed class SqliteLogStore
{
    private readonly string _connectionString;
    private readonly HelperLoggingOptions _options;
    private readonly LogBuffer _buffer;

    internal SqliteLogStore(HelperLoggingOptions options, LogBuffer buffer)
    {
        _options = options;
        _buffer = buffer;
        var path = Path.GetFullPath(options.DatabasePath, AppContext.BaseDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            DefaultTimeout = 3,
            Pooling = true
        }.ToString();
    }

    public LoggingStatus GetStatus() => _buffer.Status();

    internal void Initialize()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS helper_logs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                timestamp INTEGER NOT NULL,
                level INTEGER NOT NULL,
                category TEXT NOT NULL,
                event_id INTEGER NOT NULL,
                event_name TEXT,
                message_template TEXT NOT NULL,
                message TEXT NOT NULL,
                exception TEXT,
                properties TEXT NOT NULL,
                scopes TEXT NOT NULL,
                trace_id TEXT,
                span_id TEXT,
                application TEXT NOT NULL,
                instance TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_helper_logs_time ON helper_logs(timestamp);
            CREATE INDEX IF NOT EXISTS ix_helper_logs_level_time ON helper_logs(level, timestamp);
            CREATE INDEX IF NOT EXISTS ix_helper_logs_trace_id ON helper_logs(trace_id, id);
            """;
        command.ExecuteNonQuery();
    }

    internal void Write(IReadOnlyList<LogEntry> entries)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO helper_logs(timestamp,level,category,event_id,event_name,message_template,message,
                exception,properties,scopes,trace_id,span_id,application,instance)
            VALUES($timestamp,$level,$category,$event_id,$event_name,$message_template,$message,
                $exception,$properties,$scopes,$trace_id,$span_id,$application,$instance)
            """;
        foreach (var name in new[] { "timestamp", "level", "category", "event_id", "event_name", "message_template",
            "message", "exception", "properties", "scopes", "trace_id", "span_id", "application", "instance" })
        {
            command.Parameters.Add(new SqliteParameter("$" + name, DBNull.Value));
        }
        foreach (var entry in entries)
        {
            Set("timestamp", entry.TimestampUtc.ToUnixTimeMilliseconds());
            Set("level", entry.Level);
            Set("category", entry.Category);
            Set("event_id", entry.EventId);
            Set("event_name", entry.EventName);
            Set("message_template", entry.MessageTemplate);
            Set("message", entry.Message);
            Set("exception", entry.Exception);
            Set("properties", entry.PropertiesJson);
            Set("scopes", entry.ScopesJson);
            Set("trace_id", entry.TraceId);
            Set("span_id", entry.SpanId);
            Set("application", entry.ApplicationName);
            Set("instance", entry.InstanceId);
            command.ExecuteNonQuery();
        }
        transaction.Commit();
        return;

        void Set(string name, object? value) => command.Parameters["$" + name].Value = value ?? DBNull.Value;
    }

    /// <summary>查询日志，返回下一页游标；不执行高开销的总条数计算。</summary>
    public async Task<LogPage> QueryAsync(LogQuery query, CancellationToken cancellationToken = default)
    {
        Validate(query);
        using var connection = Open();
        using var command = connection.CreateCommand();
        var where = new StringBuilder(" WHERE 1=1");
        if (query.From is { } from) Add("timestamp >= $from", "$from", from.ToUnixTimeMilliseconds());
        if (query.To is { } to) Add("timestamp < $to", "$to", to.ToUnixTimeMilliseconds());
        if (query.Level is { } level) Add("level = $level", "$level", level);
        if (query.MinimumLevel is { } minimum) Add("level >= $minimum", "$minimum", minimum);
        if (query.BeforeId is { } before) Add("id < $before", "$before", before);
        if (!string.IsNullOrEmpty(query.Category)) Add("category = $category", "$category", query.Category);
        if (!string.IsNullOrEmpty(query.TraceId)) Add("trace_id = $trace", "$trace", query.TraceId);
        if (!string.IsNullOrEmpty(query.Keyword))
            Add("(instr(message, $keyword) > 0 OR instr(COALESCE(exception,''), $keyword) > 0 OR instr(properties, $keyword) > 0)",
                "$keyword", query.Keyword);
        command.CommandText = "SELECT * FROM helper_logs" + where + " ORDER BY id DESC LIMIT $limit";
        command.Parameters.AddWithValue("$limit", query.PageSize + 1);
        var items = new List<LogEntry>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) items.Add(Read(reader));
        var hasMore = items.Count > query.PageSize;
        if (hasMore) items.RemoveAt(items.Count - 1);
        return new LogPage(items, hasMore ? items[^1].Id : null);

        void Add(string condition, string name, object value)
        {
            where.Append(" AND ").Append(condition);
            command.Parameters.AddWithValue(name, value);
        }
    }

    public async Task<LogEntry?> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM helper_logs WHERE id=$id";
        command.Parameters.AddWithValue("$id", id);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
    }

    /// <summary>今日指标按传入的 UTC 偏移计算，趋势为最近 24 小时的 UTC 小时桶。</summary>
    public async Task<LogStatistics> GetStatisticsAsync(int utcOffsetMinutes = 0,
        CancellationToken cancellationToken = default)
    {
        if (utcOffsetMinutes is < -840 or > 840) throw new ArgumentOutOfRangeException(nameof(utcOffsetMinutes));
        var now = DateTimeOffset.UtcNow;
        var offset = TimeSpan.FromMinutes(utcOffsetMinutes);
        var today = new DateTimeOffset(now.ToOffset(offset).Date, offset).ToUnixTimeMilliseconds();
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*), COALESCE(SUM(timestamp >= $today),0),
                COALESCE(SUM(timestamp >= $today AND level >= 4),0),
                COALESCE(SUM(timestamp >= $hour),0) FROM helper_logs;
            """;
        command.Parameters.AddWithValue("$today", today);
        command.Parameters.AddWithValue("$hour", now.AddHours(-1).ToUnixTimeMilliseconds());
        long total, countToday, errors, hour;
        using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            await reader.ReadAsync(cancellationToken);
            total = reader.GetInt64(0);
            countToday = reader.GetInt64(1);
            errors = reader.GetInt64(2);
            hour = reader.GetInt64(3);
        }
        command.Parameters.Clear();
        var start = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero).AddHours(-23);
        command.CommandText = """
            SELECT timestamp / 3600000, COUNT(*), SUM(level >= 4) FROM helper_logs
            WHERE timestamp >= $start GROUP BY timestamp / 3600000;
            """;
        command.Parameters.AddWithValue("$start", start.ToUnixTimeMilliseconds());
        var buckets = new Dictionary<long, (long Total, long Errors)>();
        using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                buckets[reader.GetInt64(0)] = (reader.GetInt64(1), reader.GetInt64(2));
        }
        var trend = Enumerable.Range(0, 24).Select(i =>
        {
            var time = start.AddHours(i);
            var value = buckets.GetValueOrDefault(time.ToUnixTimeMilliseconds() / 3_600_000);
            return new LogTrendPoint(time, value.Total, value.Errors);
        }).ToArray();
        return new LogStatistics(total, countToday, errors, hour, trend);
    }

    internal int DeleteExpiredBatch()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM helper_logs WHERE id IN
                (SELECT id FROM helper_logs WHERE timestamp < $cutoff ORDER BY timestamp LIMIT 1000)
            """;
        command.Parameters.AddWithValue("$cutoff", DateTimeOffset.UtcNow.AddDays(-_options.RetentionDays).ToUnixTimeMilliseconds());
        return command.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        try
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA synchronous=NORMAL;";
            command.ExecuteNonQuery();
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    internal static void Validate(LogQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.PageSize is < 1 or > 200 || query.Level is < 0 or > 5 || query.MinimumLevel is < 0 or > 5 || query.BeforeId is <= 0 ||
            query.From > query.To || query.Keyword?.Length > 500 || query.Category?.Length > 500 || query.TraceId?.Length > 128)
            throw new ArgumentException("查询条件无效：每页 1–200 条，时间范围和日志级别须有效。");
    }

    private static LogEntry Read(SqliteDataReader r) => new()
    {
        Id = r.GetInt64(0),
        TimestampUtc = DateTimeOffset.FromUnixTimeMilliseconds(r.GetInt64(1)),
        Level = r.GetInt32(2),
        Category = r.GetString(3),
        EventId = r.GetInt32(4),
        EventName = Text(r, 5),
        MessageTemplate = r.GetString(6),
        Message = r.GetString(7),
        Exception = Text(r, 8),
        PropertiesJson = r.GetString(9),
        ScopesJson = r.GetString(10),
        TraceId = Text(r, 11),
        SpanId = Text(r, 12),
        ApplicationName = r.GetString(13),
        InstanceId = r.GetString(14)
    };

    private static string? Text(SqliteDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetString(index);
}
