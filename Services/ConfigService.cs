using System.Reflection;
using System.Text.Json;
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
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true, PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly Lock _lock = new();
    private readonly ISptLogger<ConfigService> _logger;
    private PpesConfig _saved = new();
    private PpesConfig? _applied;

    public ConfigService(ModHelper modHelper, ISptLogger<ConfigService> logger)
    {
        _logger = logger;
        ModPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        Load();
    }

    public string ModPath { get; }
    private string ConfigPath => Path.Combine(ModPath, "config.json");

    /// <summary>What is on disk now. Raid-time events read this, so they change from the next raid.</summary>
    public PpesConfig Saved => Volatile.Read(ref _saved);

    /// <summary>What the server applied at startup. Load-time events only change after a restart.</summary>
    public PpesConfig Applied => Volatile.Read(ref _applied) ?? Saved;

    public event Action? Changed;

    public void MarkApplied()
    {
        lock (_lock) Volatile.Write(ref _applied, _saved.Clone());
    }

    /// <summary>Saves a page preference (language, view, presets) right away, leaving unsaved event picks alone.</summary>
    public void Update(Action<PpesConfig> change)
    {
        var updated = Saved.Clone();
        change(updated);
        Save(updated);
    }

    public void Save(PpesConfig config)
    {
        var normalized = Normalize(config.Clone());
        lock (_lock)
        {
            WriteAtomic(normalized);
            Volatile.Write(ref _saved, normalized);
        }
        Changed?.Invoke();
    }

    private void Load()
    {
        lock (_lock)
        {
            try
            {
                var loaded = File.Exists(ConfigPath)
                    ? JsonSerializer.Deserialize<PpesConfig>(File.ReadAllText(ConfigPath), JsonOptions) ?? new PpesConfig()
                    : new PpesConfig();
                loaded = Normalize(loaded);
                if (!File.Exists(ConfigPath)) WriteAtomic(loaded);
                Volatile.Write(ref _saved, loaded);
            }
            catch (Exception ex)
            {
                var backup = ConfigPath + ".broken-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json";
                try { File.Copy(ConfigPath, backup, true); }
                catch { backup = "(backup failed)"; }
                _logger.Error($"{LogPrefix} Invalid config.json, using defaults. Backup: {backup}. Error: {ex.Message}");
                Volatile.Write(ref _saved, Normalize(new PpesConfig()));
            }
        }
    }

    private static PpesConfig Normalize(PpesConfig config)
    {
        config.Language = string.Equals(config.Language, "pt", StringComparison.OrdinalIgnoreCase) ? "pt" : "en";
        config.View = config.View == "list" ? "list" : "grid";
        config.Seasonal = NormalizeSeasonal(config.Seasonal);
        config.Events = NormalizeEvents(config.Events);
        config.Halloween ??= new HalloweenParts();
        config.Christmas ??= new ChristmasParts();
        config.AprilFools ??= new AprilFoolsParts();
        config.Rotation ??= new RotationSettings();
        config.Rotation.Mode = RotationMode.Normalize(config.Rotation.Mode);
        config.Rotation.Count = Math.Clamp(config.Rotation.Count, 1, EventSchedule.RotationPool.Count());
        config.Presets = (config.Presets ?? new())
            .Where(p => !string.IsNullOrWhiteSpace(p.Key) && p.Value is not null)
            .ToDictionary(p => p.Key.Trim(), p => new PresetData { Seasonal = NormalizeSeasonal(p.Value.Seasonal ?? new()), Events = NormalizeEvents(p.Value.Events ?? []) });
        return config;
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
        File.WriteAllText(tmp, JsonSerializer.Serialize(config, JsonOptions));
        File.Move(tmp, ConfigPath, true);
    }
}
