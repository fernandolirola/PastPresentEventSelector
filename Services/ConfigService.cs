using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using PPES.Events;
using PPES.Models;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Server;

namespace PPES.Services;

[Injectable(InjectionType.Singleton)]
public sealed class ConfigService
{
    public const string LogPrefix = "[Event Selector]";

    private readonly Lock _lock = new();
    private readonly ISptLogger<ConfigService> _logger;
    private PpesConfig _saved = new();

    public ConfigService(ModHelper modHelper, ISptLogger<ConfigService> logger)
        : this(modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly()), logger)
    {
    }

    internal ConfigService(string modPath, ISptLogger<ConfigService> logger)
    {
        _logger = logger;
        ModPath = modPath;
        Load();
        Applied = Saved.Clone();
    }

    public string ModPath { get; }

    /// <summary>When the server started; seasonal events were picked for this date.</summary>
    public DateTime StartedAt { get; } = DateTimeOffset.Now.DateTime;
    private string ConfigPath => Path.Combine(ModPath, "config.json");

    /// <summary>What is on disk now. Raid-time settings read this, so they change from the next raid.</summary>
    public PpesConfig Saved => Volatile.Read(ref _saved);

    /// <summary>What the server loaded at startup. Load-time settings only change after a restart.</summary>
    public PpesConfig Applied { get; }

    public event Action? Changed;

    /// <summary>Saves a page preference (language, view, presets) right away, leaving unsaved event picks alone.</summary>
    public void Update(Action<PpesConfig> change) => Write(_ =>
    {
        var updated = Saved.Clone();
        change(updated);
        return updated;
    });

    /// <summary>Saves the page's draft unless someone else saved a different selection since it was loaded.</summary>
    public bool TrySaveDraft(PpesConfig draft, PpesConfig baseline) => Write(latest =>
    {
        if (!latest.SameSelectionAs(baseline)) return null;
        var updated = draft.Clone();
        updated.CopyPreferencesFrom(latest);
        return updated;
    });

    private bool Write(Func<PpesConfig, PpesConfig?> next)
    {
        lock (_lock)
        {
            if (next(Saved) is not { } updated) return false;
            Normalize(updated);
            WriteAtomic(updated);
            Volatile.Write(ref _saved, updated);
        }
        Changed?.Invoke();
        return true;
    }

    private void Load()
    {
        try
        {
            var text = File.Exists(ConfigPath) ? File.ReadAllText(ConfigPath) : null;
            var loaded = text is null ? new PpesConfig() : JsonSerializer.Deserialize<PpesConfig>(text, PpesConfig.JsonOptions) ?? new PpesConfig();
            var migrated = text is not null && MigrateToxin(text, loaded);
            Normalize(loaded);
            if (text is null || migrated) WriteAtomic(loaded);
            _saved = loaded;
        }
        catch (Exception ex)
        {
            var backup = ConfigPath + ".broken-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json";
            try { File.Copy(ConfigPath, backup, true); }
            catch { backup = "(backup failed)"; }
            _logger.Error($"{LogPrefix} Invalid config.json, using defaults. Backup: {backup}. Error: {ex.Message}");
            var defaults = new PpesConfig();
            Normalize(defaults);
            _saved = defaults;
        }
    }

    /// <summary>
    /// Up to 1.7.4 the toxin sliders had wrong defaults (10 and 33) and a wrong sign, so an old hydration value is
    /// dropped and the old hearing default becomes the game's 20.
    /// </summary>
    internal static bool MigrateToxin(string text, PpesConfig loaded)
    {
        var mechanics = JsonNode.Parse(text, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true })?["mechanics"] as JsonObject;
        if (mechanics is null) return false;
        bool Has(string key) => mechanics.Any(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));
        if (!Has("toxinHydration") || Has("toxinDehydration")) return false;
        if (loaded.Mechanics?.ToxinHearing == 33) loaded.Mechanics.ToxinHearing = 20;
        return true;
    }

    internal static void Normalize(PpesConfig config)
    {
        config.Language = string.Equals(config.Language, "pt", StringComparison.OrdinalIgnoreCase) ? "pt" : "en";
        config.View = config.View == "list" ? "list" : "grid";
        config.Seasonal = NormalizeSeasonal(config.Seasonal ?? new());
        config.Events = NormalizeEvents(config.Events ?? []);
        config.Halloween ??= new HalloweenParts();
        config.Christmas ??= new ChristmasParts();
        config.AprilFools ??= new AprilFoolsParts();
        config.Mechanics ??= new MechanicsSettings();
        config.Rotation ??= new RotationSettings();
        config.Rotation.Mode = RotationMode.Normalize(config.Rotation.Mode);
        config.SeasonalRotation ??= new SeasonalRotationSettings();
        config.SeasonalRotation.Mode = SeasonalRotationMode.Normalize(config.SeasonalRotation.Mode);
        foreach (var part in new object[] { config.Halloween, config.Mechanics, config.Rotation, config.SeasonalRotation })
            Settings.Normalize(part);
        config.Spawns = NormalizeSpawns(config.Spawns ?? new());
        var presets = new Dictionary<string, PresetData>();
        foreach (var (name, preset) in config.Presets ?? new())
            if (!string.IsNullOrWhiteSpace(name) && preset is not null)
                presets[name.Trim()] = new PresetData { Seasonal = NormalizeSeasonal(preset.Seasonal ?? new()), Events = NormalizeEvents(preset.Events ?? []) };
        config.Presets = presets;
    }

    /// <summary>One edit per role and map, the last one winning; edits that change nothing are dropped.</summary>
    private static Dictionary<string, List<SpawnEdit>> NormalizeSpawns(Dictionary<string, List<SpawnEdit>> spawns)
    {
        var result = new Dictionary<string, List<SpawnEdit>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (map, edits) in spawns)
        {
            if (string.IsNullOrWhiteSpace(map) || edits is null) continue;
            var byRole = new Dictionary<string, SpawnEdit>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in edits)
            {
                if (e is null || string.IsNullOrWhiteSpace(e.Role)) continue;
                e.Role = e.Role.Trim();
                e.Chance = e.Chance is { } c ? Math.Clamp(Math.Round(c, 1), 0, 100) : null;
                e.Zones = ZoneList.Join(ZoneList.Split(e.Zones));
                e.Escort = SpawnEdits.NormalizeEscort(e.Escort);
                if (e.ChangesNothing) continue;
                byRole[e.Role] = e;
            }
            if (byRole.Count > 0) result[map.Trim().ToLowerInvariant()] = [.. byRole.Values];
        }
        return result;
    }

    private static Dictionary<string, string> NormalizeSeasonal(Dictionary<string, string> modes)
    {
        var seasonal = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in EventCatalog.Seasonal)
            seasonal[s.Id] = SeasonalMode.Normalize(modes.GetValueOrDefault(s.Id));
        return seasonal;
    }

    /// <summary>Keeps catalog order, drops unknown ids, and lets only the last pick of an exclusive group survive.</summary>
    public static List<string> NormalizeEvents(IEnumerable<string> events)
    {
        var picked = events.Where(EventCatalog.IsKnownPast).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var byGroup = new Dictionary<string, string>();
        foreach (var id in picked)
            if (EventCatalog.Past(id)?.Group is { } group) byGroup[group] = id;
        return EventCatalog.PastEvents
            .Where(e => picked.Contains(e.Id, StringComparer.OrdinalIgnoreCase))
            .Where(e => e.Group is null || string.Equals(byGroup[e.Group], e.Id, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Id)
            .ToList();
    }

    private void WriteAtomic(PpesConfig config)
    {
        var tmp = ConfigPath + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(config, PpesConfig.JsonOptions));
        File.Move(tmp, ConfigPath, true);
    }
}
