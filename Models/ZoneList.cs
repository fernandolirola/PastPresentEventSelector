namespace PPES.Models;

/// <summary>SPT keeps zone lists as comma-separated text.</summary>
public static class ZoneList
{
    public static string[] Split(string? zones) =>
        (zones ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    /// <summary>Null for an empty list, which means "keep the map's zones" in a spawn edit.</summary>
    public static string? Join(IEnumerable<string> zones) => string.Join(",", zones) is { Length: > 0 } text ? text : null;
}
