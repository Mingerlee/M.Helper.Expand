using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace M.Simple.EntityFrameworkCore;

public interface IEntity<TKey> { TKey Id { get; set; } }
public abstract class Entity<TKey> : IEntity<TKey> { public TKey Id { get; set; } = default!; }
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    /// <summary>Begins a unit-of-work-owned transaction. Existing transactions cannot be nested or adopted.</summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    /// <summary>Commits the owned transaction without saving pending changes.</summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    /// <summary>Rolls back the owned transaction, then detaches all tracked entities, discarding pending changes. Does not restore object values.</summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    /// <summary>Requires an owned transaction, saves changes, then commits. On failure the caller must roll back.</summary>
    Task<int> SaveChangesAndCommitAsync(CancellationToken cancellationToken = default);
}
public interface IRepository<TEntity, TKey> where TEntity : class, IEntity<TKey>
{
    IQueryable<TEntity> Query(bool tracking = false);
    Task<TEntity?> GetByIdAsync(TKey id, bool tracking = false, CancellationToken cancellationToken = default);
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool tracking = false, CancellationToken cancellationToken = default);
    Task<List<TEntity>> ListAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);
    void Add(TEntity entity);
    void AddRange(IEnumerable<TEntity> entities);
    void UpdateProperties(TEntity entity, params Expression<Func<TEntity, object?>>[] properties);
    void Remove(TEntity entity);
    void RemoveRange(IEnumerable<TEntity> entities);
    Task<bool> RemoveByIdAsync(TKey id, CancellationToken cancellationToken = default);
}
/// <summary>Immediate native bulk insert; does not track entities or return generated keys.</summary>
public interface IBulkWriter
{
    Task BulkInsertAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) where TEntity : class;
}
public interface ISimpleDbContext { DbContext Context { get; } }
internal sealed class SimpleDbContext<TContext>(TContext context) : ISimpleDbContext where TContext : DbContext
{ public DbContext Context => context; }
public sealed class SimpleEfCoreOptions
{
    public int MaxPageSize { get; set; } = 200;
}
