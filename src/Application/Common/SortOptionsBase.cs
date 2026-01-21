using Application.Common.Abstractions.Persistence;

namespace Application.Common;

public abstract record SortOptionsBase
{
    public string? Sort { get; init; } = null;
    
    public SortOption SortOption => ToSortOption();
    
    /// <summary>
    /// Parses the query string value (e.g. "Name:desc") into a <see cref="SortOption"/> object.
    /// Defaults to sorting by "Name" ascending when null, empty, or invalid.
    /// </summary>
    /// <returns>
    /// A <see cref="SortOption"/> containing the target property and sort direction.
    /// </returns>
    private SortOption ToSortOption()
    {

        if (string.IsNullOrWhiteSpace(Sort))
            return new SortOption("Name");
        
        var parts = Sort.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var prop = parts.ElementAtOrDefault(0) ?? "Name";
        var dir =  parts.ElementAtOrDefault(1)?.ToLowerInvariant() ?? "desc";
        var desc = dir is "desc" or "d" or "descending";
        
        return new SortOption(prop, desc);
    }
}