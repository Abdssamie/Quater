using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Quater.Backend.Data.Interfaces;
using Quater.Shared.Models;

namespace Quater.Backend.Data.Repositories;

/// <summary>
/// Implementation of the Unit of Work pattern for coordinating database operations.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly QuaterDbContext _context;
    private IDbContextTransaction? _transaction;

    // Lazy-loaded repositories
    private IRepository<Lab>? _labs;
    private IRepository<Sample>? _samples;
    private IRepository<TestResult>? _testResults;
    private IRepository<Parameter>? _parameters;
    private IRepository<SyncLog>? _syncLogs;
    private IRepository<AuditLog>? _auditLogs;
    private IRepository<AuditLogArchive>? _auditLogArchives;
    private IRepository<ConflictBackup>? _conflictBackups;

    public UnitOfWork(QuaterDbContext context)
    {
        _context = context;
    }

    public IRepository<Lab> Labs => _labs ??= new Repository<Lab>(_context);
    public IRepository<Sample> Samples => _samples ??= new Repository<Sample>(_context);
    public IRepository<TestResult> TestResults => _testResults ??= new Repository<TestResult>(_context);
    public IRepository<Parameter> Parameters => _parameters ??= new Repository<Parameter>(_context);
    public IRepository<SyncLog> SyncLogs => _syncLogs ??= new Repository<SyncLog>(_context);
    public IRepository<AuditLog> AuditLogs => _auditLogs ??= new Repository<AuditLog>(_context);
    public IRepository<AuditLogArchive> AuditLogArchives => _auditLogArchives ??= new Repository<AuditLogArchive>(_context);
    public IRepository<ConflictBackup> ConflictBackups => _conflictBackups ??= new Repository<ConflictBackup>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
            
            // Clear the ChangeTracker to remove cached entities after rollback
            // This ensures subsequent queries go to the database instead of returning cached entities
            _context.ChangeTracker.Clear();
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}

/// <summary>
/// Generic repository implementation for basic CRUD operations.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public class Repository<T> : IRepository<T> where T : class
{
    private readonly QuaterDbContext _context;
    private readonly DbSet<T> _dbSet;

    public Repository(QuaterDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public void Remove(T entity)
    {
        _dbSet.Remove(entity);
    }

    public void RemoveRange(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
    }
}
