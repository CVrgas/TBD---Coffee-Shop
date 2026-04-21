using Application.Common.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public static class QueryableSortExtensions
{
    /// <summary>
    /// Apply a single sort (or none if invalid/null). Case-insensitive property match.
    /// </summary>
    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> query,
        SortOption? sort)
    {
        if (sort is null || string.IsNullOrWhiteSpace(sort.Property))
            return query;

        // Find the actual property on T ignoring case
        var prop = typeof(T).GetProperties()
            .FirstOrDefault(p => string.Equals(p.Name, sort.Property, StringComparison.OrdinalIgnoreCase));

        if (prop is null) // unknown property => no-op
            return query;

        return sort.Desc
            ? query.OrderByDescending(x => EF.Property<object>(x, prop.Name))
            : query.OrderBy(x => EF.Property<object>(x, prop.Name));
    }
}