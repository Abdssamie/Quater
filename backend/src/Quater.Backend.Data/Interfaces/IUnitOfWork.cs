using Quater.Shared.Models;

namespace Quater.Backend.Data.Interfaces;

/// <summary>
/// Unit of Work pattern interface for managing database transactions.
/// Provides a single point of coordination for multiple repository operations.
/// </summary>
/// <remarks>
/// The Unit of Work pattern:
/// - Maintains a list of objects affected by a business transaction
/// - Coordinates the writing out of changes and resolves concurrency problems
/// - Ensures all repository operations succeed or fail as a single unit
/// 
/// Usage example:
/// <code>
/// using var unitOfWork = serviceProvider.GetRequiredService&lt;IUnitOfWork&gt;();
/// 
/// var sample = await unitOfWork.Samples.GetByIdAsync(sampleId);
/// sample.Status = SampleStatus.Completed;
/// 
/// var testResult = new TestResult { ... };
/// await unitOfWork.TestResults.AddAsync(testResult);
/// 
/// await unitOfWork.SaveChangesAsync(); // Commits all changes in a single transaction
/// </code>
/// </remarks>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Repository for Lab entities.
    /// </summary>
    IRepository<Lab> Labs { get; }

    /// <summary>
    /// Repository for Sample entities.
    /// </summary>
    IRepository<Sample> Samples { get; }

    /// <summary>
    /// Repository for TestResult entities.
    /// </summary>
    IRepository<TestResult> TestResults { get; }

    /// <summary>
    /// Repository for Parameter entities.
    /// </summary>
    IRepository<Parameter> Parameters { get; }



    /// <summary>
    /// Repository for AuditLog entities.
    /// </summary>
    IRepository<AuditLog> AuditLogs { get; }

    /// <summary>
    /// Repository for AuditLogArchive entities.
    /// </summary>
    IRepository<AuditLogArchive> AuditLogArchives { get; }



    /// <summary>
    /// Saves all pending changes to the database.
    /// </summary>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    /// <returns>A transaction object that can be committed or rolled back.</returns>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic repository interface for basic CRUD operations.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Gets an entity by its ID.
    /// </summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities.
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity.
    /// </summary>
    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities.
    /// </summary>
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    void Update(T entity);

    /// <summary>
    /// Removes an entity.
    /// </summary>
    void Remove(T entity);

    /// <summary>
    /// Removes multiple entities.
    /// </summary>
    void RemoveRange(IEnumerable<T> entities);
}
