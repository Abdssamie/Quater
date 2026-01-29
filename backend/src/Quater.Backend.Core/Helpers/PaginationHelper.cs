using Microsoft.EntityFrameworkCore;

namespace Quater.Backend.Core.Helpers;

/// <summary>
/// Helper class for creating paginated queries.
/// </summary>
public static class PaginationHelper
{
    /// <summary>
    /// Default page size if not specified.
    /// </summary>
    public const int DefaultPageSize = 20;

    /// <summary>
    /// Maximum allowed page size.
    /// </summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// Applies pagination to a queryable source.
    /// </summary>
    /// <typeparam name="T">The type of items in the query.</typeparam>
    /// <param name="source">The queryable source.</param>
    /// <param name="pageNumber">The page number (1-based). Defaults to 1.</param>
    /// <param name="pageSize">The page size. Defaults to 20, max 100.</param>
    /// <returns>A paginated queryable.</returns>
    public static IQueryable<T> Paginate<T>(this IQueryable<T> source, int pageNumber = 1, int pageSize = DefaultPageSize)
    {
        // Validate and normalize parameters
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        // Apply pagination
        return source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);
    }

    /// <summary>
    /// Creates a paged response from a queryable source.
    /// </summary>
    /// <typeparam name="T">The type of items in the query.</typeparam>
    /// <param name="source">The queryable source.</param>
    /// <param name="pageNumber">The page number (1-based). Defaults to 1.</param>
    /// <param name="pageSize">The page size. Defaults to 20, max 100.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged response with items and metadata.</returns>
    public static async Task<Models.PagedResponse<T>> ToPagedResponseAsync<T>(
        this IQueryable<T> source,
        int pageNumber = 1,
        int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        // Validate and normalize parameters
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        // Get total count
        var totalCount = await source.CountAsync(cancellationToken);

        // Get items for current page
        var items = await source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new Models.PagedResponse<T>(items, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Creates a paged response from a list of items.
    /// </summary>
    /// <typeparam name="T">The type of items.</typeparam>
    /// <param name="items">The items for the current page.</param>
    /// <param name="totalCount">The total number of items across all pages.</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>A paged response with items and metadata.</returns>
    public static Models.PagedResponse<T> ToPagedResponse<T>(
        this IEnumerable<T> items,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        return new Models.PagedResponse<T>(items, totalCount, pageNumber, pageSize);
    }
}
