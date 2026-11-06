using System.Globalization;
using PPES.Events;
using PPES.Models;

namespace PPES.Web;

public sealed class NewSpawn
{
    public string Role { get; set; } = "";
    public double Chance { get; set; } = 100;
    public string Zones { get; set; } = "";
    public string Escort { get; set; } = "";

    public void Clear()
    {
        Role = Zones = Escort = "";
        Chance = 100;
    }
}

/// <summary>The raid editor's changes to a selection's spawn edits on one map. An edit left changing nothing is dropped.</summary>
public static class SpawnEditor
{
    public static SpawnEdit EditOf(PpesConfig config, string map, string role)
    {
        if (!config.Spawns.TryGetValue(map, out var edits)) config.Spawns[map] = edits = [];
        var edit = edits.FirstOrDefault(e => Same(e.Role, role));
        if (edit is null) edits.Add(edit = new SpawnEdit { Role = role });
        return edit;
    }

    public static void SetChance(PpesConfig config, string map, string role, string? text)
    {
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var chance)) EditOf(config, map, role).Chance = Math.Clamp(chance, 0, 100);
    }

    /// <summary>An empty list keeps the map's own zones.</summary>
    public static void SetZones(PpesConfig config, string map, string role, IEnumerable<string> zones)
    {
        EditOf(config, map, role).Zones = ZoneList.Join(zones);
        Tidy(config, map, role);
    }

    public static void AddZone(PpesConfig config, string map, string role, string? zone, string currentZones)
    {
        if (string.IsNullOrWhiteSpace(zone)) return;
        SetZones(config, map, role, ZoneList.Split(currentZones).Append(zone).Distinct(StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>Taking the last zone away takes the role off the map.</summary>
    public static void RemoveZone(PpesConfig config, string map, string role, string zone, string currentZones)
    {
        var remaining = ZoneList.Split(currentZones).Where(z => !Same(z, zone)).ToArray();
        if (remaining.Length > 0) SetZones(config, map, role, remaining);
        else RemoveRole(config, map, role);
    }

    /// <summary>False when the text is not a number or a range; an empty text clears the escort change.</summary>
    public static bool SetEscort(PpesConfig config, string map, string role, string? text)
    {
        var escort = SpawnEdits.NormalizeEscort(text);
        if (escort is null && !string.IsNullOrWhiteSpace(text)) return false;
        EditOf(config, map, role).Escort = escort;
        Tidy(config, map, role);
        return true;
    }

    public static void RemoveRole(PpesConfig config, string map, string role)
    {
        var edit = EditOf(config, map, role);
        edit.Remove = true;
        edit.Chance = null;
        edit.Zones = null;
        edit.Escort = null;
    }

    public static void ResetRole(PpesConfig config, string map, string role) =>
        config.Spawns.GetValueOrDefault(map)?.RemoveAll(e => Same(e.Role, role));

    public static void ResetMap(PpesConfig config, string map) => config.Spawns.Remove(map);

    /// <summary>False when the chance or escort cannot be read; nothing changes then.</summary>
    public static bool Add(PpesConfig config, string map, NewSpawn spawn)
    {
        if (spawn.Role.Length == 0) return false;
        var escort = SpawnEdits.NormalizeEscort(spawn.Escort);
        if ((!string.IsNullOrWhiteSpace(spawn.Escort) && escort is null) || !double.IsFinite(spawn.Chance)) return false;
        ResetRole(config, map, spawn.Role);
        var edit = EditOf(config, map, spawn.Role);
        edit.Chance = Math.Clamp(spawn.Chance, 1, 100);
        edit.Zones = ZoneList.Join(ZoneList.Split(spawn.Zones));
        edit.Escort = escort;
        spawn.Clear();
        return true;
    }

    private static void Tidy(PpesConfig config, string map, string role)
    {
        if (EditOf(config, map, role).ChangesNothing) ResetRole(config, map, role);
    }

    private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
