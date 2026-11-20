using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace M.Simple.EntityFrameworkCore;

internal sealed class EfUnitOfWork(ISimpleDbContext accessor) : IUnitOfWork, IDisposable, IAsyncDisposable
{
    private IDbContextTransaction? _transaction;
    private bool _rollbackRequired;
    private bool _disposed;

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (_transaction is not null || accessor.Context.Database.CurrentTransaction is not null
            || System.Transactions.Transaction.Current is not null)
            throw new InvalidOperationException("已存在事务，不支持重复开启、嵌套或接管外部事务。");
        _transaction = await accessor.Context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        _rollbackRequired = false;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (_transaction is not null) RequireTransaction(forCommit: true);
        try { return await accessor.Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false); }
        catch
        {
            if (_transaction is not null) _rollbackRequired = true;
            throw;
        }
    }

    public async Task<int> SaveChangesAndCommitAsync(CancellationToken cancellationToken = default)
    {
        // Validate ownership before SaveChanges can write anything.
        RequireTransaction(forCommit: true);
        var affected = await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
        return affected;
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = RequireTransaction(forCommit: true);
        try { await transaction.CommitAsync(cancellationToken).ConfigureAwait(false); }
        catch { _rollbackRequired = true; throw; }
        // Commit completed; disposal failure must not make this transaction appear retryable.
        _transaction = null;
        await transaction.DisposeAsync().ConfigureAwait(false);
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = RequireTransaction(forCommit: false);
        try { await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false); }
        catch { _rollbackRequired = true; throw; }
        _transaction = null;
        _rollbackRequired = false;
        await transaction.DisposeAsync().ConfigureAwait(false);
    }

    private IDbContextTransaction RequireTransaction(bool forCommit)
    {
        ThrowIfDisposed();
        if (_transaction is null)
            throw new InvalidOperationException("请先调用 BeginTransactionAsync 开启事务。");
        if (!ReferenceEquals(accessor.Context.Database.CurrentTransaction, _transaction))
            throw new InvalidOperationException("工作单元事务已被外部结束或替换，请结束当前作用域。");
        if (forCommit && _rollbackRequired)
            throw new InvalidOperationException("事务保存或提交已失败，请先回滚事务并结束当前工作作用域。");
        return _transaction;
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        var transaction = _transaction;
        _transaction = null;
        // Relational transaction disposal rolls back uncommitted changes; never commit on disposal.
        transaction?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        var transaction = _transaction;
        _transaction = null;
        if (transaction is not null) await transaction.DisposeAsync().ConfigureAwait(false);
    }
}
