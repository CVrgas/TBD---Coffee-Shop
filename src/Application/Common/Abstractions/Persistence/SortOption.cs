namespace Application.Common.Abstractions.Persistence;

/// <summary>
/// Represents a single sorting rule with a property name and direction (ascending or descending).
/// </summary>
/// <param name="Property">The name of the property to sort by (e.g. "Name", "Price").</param>
/// <param name="Desc">Indicates whether the sorting is descending. Defaults to <c>false</c> (ascending).</param>

public sealed record SortOption(string Property, bool Desc = false)
{
    /// <summary>
    /// Attempts to parse a string representation of a sort expression (e.g. "Name:desc") 
    /// into a <see cref="SortOption"/> instance.
    /// </summary>
    /// <param name="value">The input string to parse (e.g. "Name:desc" or "Price:asc").</param>
    /// <param name="result">When this method returns, contains the parsed <see cref="SortOption"/> if successful; otherwise, the default value.</param>
    /// <returns><c>true</c> if parsing was successful; otherwise, <c>false</c>.</returns>
    public static bool TryParse(string? value, out SortOption result)
    {
        result = null!;
        if (string.IsNullOrWhiteSpace(value)) return false;

        // example format: "Name:desc" or "Price:asc"
        var parts = value.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var prop = parts[0];
        var desc = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);
        result = new SortOption(prop, desc);
        return true;
    }
};