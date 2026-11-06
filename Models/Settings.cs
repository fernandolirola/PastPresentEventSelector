using System.Reflection;
using System.Text.Json;

namespace PPES.Models;

public enum Applies
{
    /// <summary>Written into the server's data at startup.</summary>
    Restart,
    /// <summary>Read when a raid is generated.</summary>
    NextRaid,
    /// <summary>Read by the client plugin while playing.</summary>
    Live,
}

/// <summary>When a changed setting takes effect. Settings without it need a restart.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AppliesAttribute(Applies when) : Attribute
{
    public Applies When { get; } = when;
}

/// <summary>Allowed range of a number setting, used by both the config file and the page.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class LimitsAttribute(double min, double max) : Attribute
{
    public double Min { get; } = min;
    public double Max { get; } = max;
    public int Decimals { get; init; } = 2;
    /// <summary>Any value below the minimum means "off" and is stored as this.</summary>
    public double Off { get; init; } = double.NaN;
    /// <summary>Another property of the same object this one may not be below.</summary>
    public string? AtLeast { get; init; }

    public double Clamp(double value) =>
        !double.IsNaN(Off) && value < Min ? Off : Math.Clamp(Math.Round(value, Decimals), Min, Max);
}

/// <summary>Reads the attributes on the flat setting classes, so each setting is declared once.</summary>
public static class Settings
{
    private static IEnumerable<PropertyInfo> Props(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.CanWrite);

    public static Applies WhenOf(PropertyInfo p) => p.GetCustomAttribute<AppliesAttribute>()?.When ?? Applies.Restart;

    public static LimitsAttribute? LimitsOf(Type type, string property) => type.GetProperty(property)?.GetCustomAttribute<LimitsAttribute>();

    /// <summary>Same values for the properties that take effect <paramref name="when"/> (all of them when null).</summary>
    public static bool Same<T>(T a, T b, Applies? when = null) where T : class =>
        Props(typeof(T)).Where(p => when is null || WhenOf(p) == when).All(p => Equals(p.GetValue(a), p.GetValue(b)));

    /// <summary>Clamps every number setting to its limits.</summary>
    public static void Normalize(object target)
    {
        foreach (var p in Props(target.GetType()))
        {
            if (p.GetCustomAttribute<LimitsAttribute>() is not { } limits) continue;
            var value = limits.Clamp(Convert.ToDouble(p.GetValue(target)));
            if (limits.AtLeast is { } floor) value = Math.Max(value, Convert.ToDouble(target.GetType().GetProperty(floor)!.GetValue(target)));
            p.SetValue(target, Convert.ChangeType(value, p.PropertyType));
        }
    }

    public static double Get(object target, string property) => Convert.ToDouble(target.GetType().GetProperty(property)!.GetValue(target));

    /// <summary>Sets one number setting from the page, clamped to its limits.</summary>
    public static void Set(object target, string property, double value)
    {
        var p = target.GetType().GetProperty(property)!;
        var limited = p.GetCustomAttribute<LimitsAttribute>() is { } limits ? limits.Clamp(value) : value;
        p.SetValue(target, Convert.ChangeType(limited, p.PropertyType));
    }

    /// <summary>The settings the client plugin reads, by camel-case name.</summary>
    public static string LiveJson(params object[] sources) =>
        JsonSerializer.Serialize(sources
            .SelectMany(s => Props(s.GetType()).Where(p => WhenOf(p) == Applies.Live).Select(p => (p, s)))
            .ToDictionary(x => JsonNamingPolicy.CamelCase.ConvertName(x.p.Name), x => x.p.GetValue(x.s)));
}
