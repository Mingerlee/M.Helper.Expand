using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace M.Simple.EntityFrameworkCore;

public sealed class EfRepository<TEntity, TKey>(ISimpleDbContext accessor) : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    private DbContext Context => accessor.Context;
    private DbSet<TEntity> Set => Context.Set<TEntity>();
    public IQueryable<TEntity> Query(bool tracking = false) => tracking ? Set : Set.AsNoTracking();
    public Task<TEntity?> GetByIdAsync(TKey id, bool tracking = false, CancellationToken cancellationToken = default)
        => Query(tracking).FirstOrDefaultAsync(x => x.Id!.Equals(id), cancellationToken);
    public Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool tracking = false, CancellationToken cancellationToken = default)
        => Query(tracking).FirstOrDefaultAsync(predicate, cancellationToken);
    public Task<List<TEntity>> ListAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => (predicate is null ? Query() : Query().Where(predicate)).ToListAsync(cancellationToken);
    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => Query().AnyAsync(predicate, cancellationToken);
    public Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => (predicate is null ? Query() : Query().Where(predicate)).CountAsync(cancellationToken);
    public void Add(TEntity entity) => Set.Add(entity);
    public void AddRange(IEnumerable<TEntity> entities) => Set.AddRange(entities);
    public void Remove(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Set.Remove(Context.Entry(entity).State == EntityState.Detached ? FindTracked(entity) ?? entity : entity);
    }
    public void RemoveRange(IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        var items = entities.ToArray();
        var tracked = Set.Local.Where(local => !Context.Entry(local).Property(nameof(IEntity<TKey>.Id)).IsTemporary)
            .ToLookup(local => local.Id);
        foreach (var entity in items)
        {
            ArgumentNullException.ThrowIfNull(entity);
            Set.Remove(Context.Entry(entity).State == EntityState.Detached
                ? tracked[entity.Id].FirstOrDefault() ?? entity : entity);
        }
    }
    private TEntity? FindTracked(TEntity entity) => Set.Local.FirstOrDefault(
        local => !Context.Entry(local).Property(nameof(IEntity<TKey>.Id)).IsTemporary
            && EqualityComparer<TKey>.Default.Equals(local.Id, entity.Id));
    public async Task<bool> RemoveByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, true, cancellationToken);
        if (entity is null) return false;
        Remove(entity);
        return true;
    }
    public void UpdateProperties(TEntity entity, params Expression<Func<TEntity, object?>>[] properties)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(properties);
        if (properties.Length == 0) throw new ArgumentException("Specify at least one property.", nameof(properties));
        var entry = Context.Entry(entity);
        if (!entry.IsKeySet) throw new ArgumentException("A persisted entity key is required.", nameof(entity));
        if (entry.State is EntityState.Added or EntityState.Deleted)
            throw new InvalidOperationException("Cannot partially update an added or deleted entity.");
        // Validate every selector before changing the tracking state.
        var names = properties.Select(selector =>
        {
            ArgumentNullException.ThrowIfNull(selector);
            Expression body = selector.Body;
            if (body is UnaryExpression { NodeType: ExpressionType.Convert } conversion) body = conversion.Operand;
            if (body is not MemberExpression { Member: PropertyInfo } member || member.Expression != selector.Parameters[0])
                throw new ArgumentException("Select a direct scalar property.", nameof(properties));
            var property = entry.Metadata.FindProperty(member.Member.Name);
            if (property is null || property.IsKey() || property.IsConcurrencyToken || property.ValueGenerated != ValueGenerated.Never
                || property.GetAfterSaveBehavior() != PropertySaveBehavior.Save)
                throw new ArgumentException("Keys, generated and concurrency properties cannot be updated here.", nameof(properties));
            return property.Name;
        }).Distinct().ToArray();
        var tracked = entry.State == EntityState.Detached ? FindTracked(entity) : null;
        var target = tracked is null ? entry : Context.Entry(tracked);
        if (target.State is EntityState.Added or EntityState.Deleted)
            throw new InvalidOperationException("Cannot partially update an added or deleted entity.");
        if (target.State == EntityState.Detached) target.State = EntityState.Unchanged;
        foreach (var name in names)
        {
            target.Property(name).CurrentValue = entry.Property(name).CurrentValue;
            target.Property(name).IsModified = true;
        }
    }
}

internal sealed class EfUnitOfWork(ISimpleDbContext accessor) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => accessor.Context.SaveChangesAsync(cancellationToken);
}
