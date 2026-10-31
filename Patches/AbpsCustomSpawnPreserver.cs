using System.Reflection;
using HarmonyLib;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils.Cloners;

namespace PPES.Patches;

/// <summary>
/// ABPS builds its map cache after the normal post-load phase. Its first step replaces every
/// BossLocationSpawn list, which removes spawn entries created earlier by MoreBotsAPI-dependent faction mods.
/// Capture only registered, non-vanilla bot roles immediately before that rebuild and merge them into both
/// ABPS's cache and the location table afterward. The same preservation is applied to ABPS's per-raid rebuild.
/// </summary>
[Injectable(TypePriority = OnLoadOrder.PostLoad)]
public sealed class AbpsCustomSpawnPreserver(
    IServiceProvider services,
    BotTable bots,
    LocationTable locations,
    ICloner cloner,
    ISptLogger<AbpsCustomSpawnPreserver> logger) : IOnLoad
{
    private static readonly string[] Maps =
    [
        "bigmap", "factory4_day", "factory4_night", "interchange", "laboratory", "lighthouse",
        "rezervbase", "sandbox", "sandbox_high", "shoreline", "tarkovstreets", "woods", "labyrinth",
    ];

    private static readonly HashSet<string> VanillaRoles =
        new(Enum.GetNames<WildSpawnType>(), StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> MoreBotsRoles = new(StringComparer.OrdinalIgnoreCase);

    private static BotTable? _bots;
    private static LocationTable? _locations;
    private static ICloner? _cloner;
    private static ISptLogger<AbpsCustomSpawnPreserver>? _logger;
    private static FieldInfo? _botMapCache;

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var abps = AccessTools.TypeByName("BotPlacementSystemServer.Controllers.MapSpawns");
        if (abps is null) return Task.CompletedTask;
        var abpsPriority = abps.GetCustomAttribute<Injectable>()?.TypePriority;
        ReadMoreBotsRoles(services);

        var configure = AccessTools.Method(abps, "ConfigureInitialData")
            ?? throw new MissingMethodException(abps.FullName, "ConfigureInitialData");
        var rebuild = AccessTools.Method(abps, "RebuildMap")
            ?? throw new MissingMethodException(abps.FullName, "RebuildMap");
        _botMapCache = AccessTools.Field(abps, "_botMapCache")
            ?? throw new MissingFieldException(abps.FullName, "_botMapCache");
        _bots = bots;
        _locations = locations;
        _cloner = cloner;
        _logger = logger;

        var harmony = new Harmony("zezinho.ppes.abps-custom-spawn-preserver");
        harmony.Patch(configure,
            prefix: new HarmonyMethod(typeof(AbpsCustomSpawnPreserver), nameof(BeforeConfigure)),
            postfix: new HarmonyMethod(typeof(AbpsCustomSpawnPreserver), nameof(AfterConfigure)));
        harmony.Patch(rebuild,
            prefix: new HarmonyMethod(typeof(AbpsCustomSpawnPreserver), nameof(BeforeRebuild)),
            postfix: new HarmonyMethod(typeof(AbpsCustomSpawnPreserver), nameof(AfterRebuild)));
        logger.Info($"[Event Selector] ABPS custom-faction spawn preservation enabled (ABPS priority {abpsPriority?.ToString() ?? "unknown"}, MoreBots roles {MoreBotsRoles.Count})");
        return Task.CompletedTask;
    }

    private static void BeforeConfigure(ref Dictionary<string, List<BossLocationSpawn>> __state)
    {
        __state = CaptureAll();
        var captured = __state.Sum(pair => pair.Value.Count);
        if (captured > 0)
            _logger?.Info($"[Event Selector] Preserving {captured} custom-faction spawn entries before ABPS rebuild");
    }

    private static void AfterConfigure(object __instance, Dictionary<string, List<BossLocationSpawn>> __state)
    {
        var restored = 0;
        foreach (var (map, spawns) in __state)
            restored += Restore(__instance, map, spawns);
        if (restored > 0)
            _logger?.Info($"[Event Selector] Restored {restored} custom-faction spawn entries after ABPS startup rebuild");
    }

    private static void BeforeRebuild(string location, ref List<BossLocationSpawn> __state)
    {
        __state = Capture(location);
    }

    private static void AfterRebuild(object __instance, string location, List<BossLocationSpawn> __state)
    {
        var restored = Restore(__instance, location, __state);
        if (restored > 0)
            _logger?.Info($"[Event Selector] Restored {restored} custom-faction spawn entries on {location} after ABPS raid rebuild");
    }

    private static Dictionary<string, List<BossLocationSpawn>> CaptureAll()
    {
        var result = new Dictionary<string, List<BossLocationSpawn>>(StringComparer.OrdinalIgnoreCase);
        foreach (var map in Maps)
        {
            var spawns = Capture(map);
            if (spawns.Count > 0) result[map] = spawns;
        }
        return result;
    }

    private static List<BossLocationSpawn> Capture(string map)
    {
        if (_bots is null || _locations is null || _cloner is null) return [];
        var key = _locations.GetMappedKey(map);
        if (!_locations.GetDictionary().TryGetValue(key, out var location)) return [];
        var custom = location.Base.BossLocationSpawn
            .Where(spawn => IsRegisteredCustomRole(spawn.BossName))
            .ToList();
        return custom.Count == 0 ? [] : _cloner.Clone<List<BossLocationSpawn>>(custom) ?? [];
    }

    private static bool IsRegisteredCustomRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role) || _bots is null) return false;
        if (MoreBotsRoles.Contains(role)) return true;
        if (Enum.TryParse<WildSpawnType>(role, true, out var parsed) && (int)parsed >= 1000) return true;
        if (VanillaRoles.Contains(role)) return false;
        return _bots.Types.ContainsKey(role.ToLowerInvariant());
    }

    private static void ReadMoreBotsRoles(IServiceProvider services)
    {
        var type = AccessTools.TypeByName("MoreBotsServer.Services.MoreBotsCustomBotTypeService");
        var service = type is null ? null : services.GetService(type);
        var property = type is null ? null : AccessTools.Property(type, "CustomWildSpawnTypes");
        if (service is null || property?.GetValue(service) is not System.Collections.IDictionary roles) return;
        foreach (var value in roles.Values)
            if (value is string role && !string.IsNullOrWhiteSpace(role)) MoreBotsRoles.Add(role);
    }

    private static int Restore(object abps, string map, List<BossLocationSpawn> saved)
    {
        if (saved.Count == 0 || _locations is null || _cloner is null || _botMapCache is null) return 0;
        var cache = _botMapCache.GetValue(abps) as Dictionary<string, List<BossLocationSpawn>>;
        if (cache is null || !cache.TryGetValue(map, out var cached)) return 0;
        var key = _locations.GetMappedKey(map);
        if (!_locations.GetDictionary().TryGetValue(key, out var location)) return 0;

        var added = Merge(cached, saved);
        Merge(location.Base.BossLocationSpawn, saved);
        return added;
    }

    private static int Merge(List<BossLocationSpawn> target, List<BossLocationSpawn> saved)
    {
        if (_cloner is null) return 0;
        var added = 0;
        foreach (var spawn in saved)
        {
            if (target.Any(existing => SameSpawn(existing, spawn))) continue;
            var clone = _cloner.Clone(spawn);
            if (clone is null) continue;
            target.Add(clone);
            added++;
        }
        return added;
    }

    private static bool SameSpawn(BossLocationSpawn left, BossLocationSpawn right) =>
        string.Equals(left.BossName, right.BossName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(left.BossZone, right.BossZone, StringComparison.OrdinalIgnoreCase)
        && left.Time == right.Time
        && string.Equals(left.BossEscortType, right.BossEscortType, StringComparison.OrdinalIgnoreCase)
        && string.Equals(left.BossEscortAmount, right.BossEscortAmount, StringComparison.OrdinalIgnoreCase);
}
