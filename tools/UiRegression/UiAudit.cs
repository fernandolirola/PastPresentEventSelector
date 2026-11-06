using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using PPES.Models;
using PPES.Services;
using PPES.Web;
using Settings = PPES.Models.Settings;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils.Cloners;

internal static class UiAudit
{
    private static int _checks;

    private static void Check(bool condition, string title)
    {
        if (!condition) throw new InvalidOperationException(title);
        Console.WriteLine("PASS " + title);
        _checks++;
    }

    public static void Run()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "audit-config");
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
        Directory.CreateDirectory(directory);
        var config = new ConfigService(directory, DispatchProxy.Create<ISptLogger<ConfigService>, Silent>());

        Saving(config, directory);
        Restart(config);
        Editing(config);
        Previews(config);
        Migration();
        ClientSettings();
        Console.WriteLine($"{_checks} UI audit checks passed.");
    }

    private static void Saving(ConfigService config, string directory)
    {
        var baseline = config.Saved.Clone();
        var draft = baseline.Clone();
        draft.Halloween.BotsHideFromZombies = true;
        config.Update(c => { c.Language = "pt"; c.View = "list"; c.Presets["Keep"] = new PresetData(); });
        Check(config.TrySaveDraft(draft, baseline), "Display preferences do not invalidate an editable draft");
        Check(config.Saved.Language == "pt" && config.Saved.View == "list" && config.Saved.Presets.ContainsKey("Keep"), "Draft save preserves latest preferences and named presets");
        Check(config.Saved.Halloween.BotsHideFromZombies, "Draft selection is saved");

        baseline = config.Saved.Clone();
        draft = baseline.Clone();
        draft.TraderMessage = false;
        config.Update(c => c.Mechanics.KollontayJam = 8);
        var fileBefore = File.ReadAllText(Path.Combine(directory, "config.json"));
        Check(!config.TrySaveDraft(draft, baseline), "Stale draft is rejected after another editor saves");
        Check(config.Saved.TraderMessage && config.Saved.Mechanics.KollontayJam == 8, "Rejecting a stale draft preserves the current selection");
        Check(File.ReadAllText(Path.Combine(directory, "config.json")) == fileBefore, "Rejected save leaves the persisted config unchanged");

        baseline = config.Saved.Clone();
        var results = new bool[2];
        using var start = new Barrier(2);
        Parallel.For(0, 2, index =>
        {
            var competing = baseline.Clone();
            competing.Mechanics.KollontayJam = 10 + index;
            start.SignalAndWait();
            results[index] = config.TrySaveDraft(competing, baseline);
        });
        Check(results.Count(r => r) == 1, "Concurrent draft saves accept exactly one baseline");
        var value = config.Saved.Mechanics.KollontayJam;
        Parallel.For(0, 20, _ => config.Update(c => c.Mechanics.KollontayJam++));
        Check(config.Saved.Mechanics.KollontayJam == value + 20, "Concurrent immediate updates do not lose changes");

        config.Update(c => c.Rotation.Count = 40);
        Check(config.Saved.Rotation.Count == 40, "Rotation size is not cut down to the events known before mods load");
    }

    private static void Restart(ConfigService config)
    {
        var calendar = new SeasonalCalendar();
        IReadOnlyList<(string En, string Pt)> Pending() => RestartCheck.Pending(config.Saved, config.Applied, calendar, config.StartedAt.Date, DateTime.Today);
        config.Update(c =>
        {
            c.Halloween = new HalloweenParts();
            c.Mechanics = new MechanicsSettings();
            c.Mechanics.KollontayJam = 30;
            c.Mechanics.VengefulJam = 0;
            c.Halloween.BotsHideFromZombies = !c.Halloween.BotsHideFromZombies;
        });
        Check(Pending().Count == 0, "Client-read settings alone do not require restart");
        config.Update(c => { c.Halloween.ZombieWaves = true; c.Halloween.ZombieWaveSizeMax = 9; });
        Check(Pending().Count == 0, "Zombie wave settings apply from the next raid without restart");
        config.Update(c => c.Mechanics.ToxinDehydration = 1.5);
        Check(Pending().Any(n => n.En.Contains("Ritual")), "Server mechanics changes appear in restart warning");
        config.Update(c => { c.Mechanics.ToxinDehydration = config.Applied.Mechanics.ToxinDehydration; c.SeasonalRotation.Mode = SeasonalRotationMode.Weekly; });
        Check(Pending().Count > 0, "Seasonal rotation change requires restart even during a break");
        config.Update(c => c.SeasonalRotation.Mode = config.Applied.SeasonalRotation.Mode);
    }

    private static void Editing(ConfigService config)
    {
        var draft = new Draft(config);
        var working = draft.Working;
        Check(!SpawnEditor.Add(working, "bigmap", new NewSpawn { Role = "bossTagilla", Escort = "invalid" }) && working.SpawnsOf("bigmap").Count == 0,
            "Invalid new-spawn escort does not change the draft");
        Check(SpawnEditor.Add(working, "bigmap", new NewSpawn { Role = "bossTagilla", Escort = "1-3" }) && working.SpawnsOf("bigmap").Single().Escort == "1-3",
            "Valid escort range is added");
        SpawnEditor.SetEscort(working, "bigmap", "bossTagilla", "");
        var edit = working.SpawnsOf("bigmap").Single();
        Check(edit.Escort is null && edit.Chance == 100, "Clearing escort keeps other spawn changes");
        Check(!SpawnEditor.SetEscort(working, "bigmap", "bossTagilla", "lots"), "Unreadable escort is refused");
        SpawnEditor.ResetRole(working, "bigmap", "bossTagilla");
        SpawnEditor.SetEscort(working, "bigmap", "bossTagilla", "2");
        SpawnEditor.SetEscort(working, "bigmap", "bossTagilla", "");
        Check(working.SpawnsOf("bigmap").Count == 0, "Clearing the only change removes the empty spawn edit");
        SpawnEditor.RemoveZone(working, "bigmap", "bossTagilla", "ZoneA", "ZoneA");
        Check(working.SpawnsOf("bigmap").Single().Remove, "Removing the last zone takes the role off the map");

        draft.NewSpawn.Escort = "unfinished";
        draft.NewSpawn.Role = "bossKilla";
        draft.Edit(w => w.TraderMessage = !w.TraderMessage);
        Check(draft.IsDirty, "An edit marks the draft as unsaved");
        draft.Discard();
        Check(draft.NewSpawn.Escort == "" && draft.NewSpawn.Role == "" && !draft.IsDirty && draft.Working.SpawnsOf("bigmap").Count == 0,
            "Discard clears the draft and the unfinished add-spawn form");
    }

    private static void Previews(ConfigService config)
    {
        var map = (LocationBase)RuntimeHelpers.GetUninitializedObject(typeof(LocationBase));
        var boss = (BossLocationSpawn)RuntimeHelpers.GetUninitializedObject(typeof(BossLocationSpawn));
        boss.BossName = "bossTagilla";
        boss.BossChance = 20;
        boss.BossZone = "ZoneTest";
        boss.BossEscortAmount = "2";
        map.BossLocationSpawn = [boss];
        map.AccessKeys = ["card"];
        map.AccessKeysPvE = ["card"];
        var maps = (LocationTable)RuntimeHelpers.GetUninitializedObject(typeof(LocationTable));
        typeof(LocationTable).GetField("_locationDictionaryCache", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(maps, new Dictionary<string, Location> { [maps.GetMappedKey("laboratory")] = new Location { Base = map } });
        var seasonal = (SeasonalEventConfig)RuntimeHelpers.GetUninitializedObject(typeof(SeasonalEventConfig));
        seasonal.EventBossSpawns = [];
        var calendar = new SeasonalCalendar();
        calendar.Capture([new SeasonalEvent
        {
            Name = "halloween", Type = SeasonalEventType.Halloween, Enabled = true, StartMonth = 10, StartDay = 24, EndMonth = 11, EndDay = 4,
            Settings = new SeasonalEventSettings { RemoveEntryRequirement = ["laboratory"] },
        }]);
        var previewer = new PreviewService(maps, new FixtureCloner(), calendar, seasonal, config, new EventSchedule(config));

        var selection = new PpesConfig { Seasonal = new(StringComparer.OrdinalIgnoreCase) { ["halloween"] = SeasonalMode.Off } };
        RaidPreview Preview(params SpawnEdit[] edits)
        {
            selection.Spawns["laboratory"] = [.. edits];
            return previewer.Preview("laboratory", selection);
        }
        Check(Preview().NeedsKeycard == true, "Preview honors stock access requirements");
        map.AccessKeys = [];
        map.AccessKeysPvE = [];
        Check(Preview().NeedsKeycard == false, "Preview honors already-free access in map data");
        map.AccessKeys = ["card"];
        selection.Seasonal["halloween"] = SeasonalMode.On;
        selection.Halloween.FreeLab = true;
        Check(Preview().NeedsKeycard == false, "Halloween free Lab appears in preview");
        var before = selection.SelectionKey();
        selection.Halloween.FreeLab = false;
        Check(Preview().NeedsKeycard == true, "Turning free Lab off restores the keycard");
        Check(selection.SelectionKey() != before, "Part settings change the selection key the preview cache uses");
        selection.Events = ["freeLabs"];
        Check(Preview().NeedsKeycard == false, "An event that frees the Lab appears in preview");
        selection.Events = [];

        var escorted = Preview(new SpawnEdit { Role = "bossTagilla", Escort = "4" });
        Check(escorted.Spawns.Single().Changed && escorted.Spawns.Single().EscortMax == 4, "Escort-only edits are marked as changed");
        Check(map.BossLocationSpawn.Single().BossEscortAmount == "2", "Preview leaves original escort data untouched");
        Check(Preview(new SpawnEdit { Role = "bossTagilla", Zones = "ZoneNew" }).Spawns.SingleOrDefault()?.RawZones == "ZoneNew", "Moving a boss to only new zones keeps the boss");
        Check(Preview(new SpawnEdit { Role = "bossTagilla", Zones = "ZoneTest,ZoneNew" }).Spawns.Single().RawZones == "ZoneTest,ZoneNew", "Adding a zone keeps the existing one");
        var removed = Preview(new SpawnEdit { Role = "bossTagilla", Remove = true });
        Check(removed.Spawns.Count == 0 && removed.Removed.Single().Role == "bossTagilla", "A removed boss is listed as removed");
    }

    private static void Migration()
    {
        var old = new PpesConfig { Mechanics = { ToxinHearing = 33 } };
        Check(ConfigService.MigrateToxin("{ \"mechanics\": { \"toxinHydration\": 10, \"toxinHearing\": 33 } }", old) && old.Mechanics.ToxinHearing == 20,
            "Old toxin defaults migrate to the game's values");
        var current = new PpesConfig { Mechanics = { ToxinHearing = 33 } };
        Check(!ConfigService.MigrateToxin("{ \"mechanics\": { \"toxinDehydration\": 0.84, \"toxinHearing\": 33 } }", current) && current.Mechanics.ToxinHearing == 33,
            "A hearing value chosen with the new settings is kept");
    }

    private static void ClientSettings()
    {
        var keys = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(Settings.LiveJson(new MechanicsSettings(), new HalloweenParts()))!.Keys;
        Check(keys.Order().SequenceEqual(["botsHideFromZombies", "kollontayJam", "vengefulJam"]), "The client gets exactly the keys its plugin reads");
    }
}

internal class FixtureCloner : ICloner
{
    public T? Clone<T>(T? value) => value switch
    {
        LocationBase map => (T)(object)(map with { BossLocationSpawn = map.BossLocationSpawn?.Select(b => b with { }).ToList() ?? [] }),
        BossLocationSpawn spawn => (T)(object)(spawn with { }),
        _ => throw new InvalidOperationException("Unexpected fixture type"),
    };
}

public class Silent : DispatchProxy
{
    protected override object? Invoke(MethodInfo? method, object?[]? args) => null;
}
