using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace M.Simple.EntityFrameworkCore;

public sealed record PageResult<T>(IReadOnlyList<T> Items, int TotalCount, int PageNumber, int PageSize)
{
    public long TotalPages => (TotalCount + (long)PageSize - 1) / PageSize;
}

public static class QueryExtensions
{
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> source, bool condition, Expression<Func<T, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);
        return condition ? source.Where(predicate) : source;
    }

    /// <summary>Requires stable ordering including a unique tie breaker. Count and rows are separate queries.</summary>
    public static async Task<PageResult<T>> ToPageAsync<T>(this IQueryable<T> source,
        int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default,
        SimpleEfCoreOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        var max = options?.MaxPageSize ?? 200;
        if (max < 1) throw new ArgumentOutOfRangeException(nameof(options));
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        if (pageSize < 1 || pageSize > max) throw new ArgumentOutOfRangeException(nameof(pageSize));
        var skip = checked((pageNumber - 1) * pageSize);
        var total = await source.CountAsync(cancellationToken);
        var items = await source.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);
        return new(items, total, pageNumber, pageSize);
    }
}
