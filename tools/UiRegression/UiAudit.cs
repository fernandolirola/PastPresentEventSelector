using System.Reflection;
using System.Runtime.CompilerServices;
using MudBlazor;
using PPES.Models;
using PPES.Services;
using PPES.Web.Pages;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils.Cloners;

internal static class UiAudit
{
    public static void Run()
    {
        var checks = 0;
        void Check(bool condition, string title)
        {
            if (!condition) throw new InvalidOperationException(title);
            Console.WriteLine("PASS " + title);
            checks++;
        }
        var config = (ConfigService)RuntimeHelpers.GetUninitializedObject(typeof(ConfigService));
        void Field(object target, string name, object? value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
        var directory = Path.Combine(AppContext.BaseDirectory, "audit-config");
        Directory.CreateDirectory(directory);
        Field(config, "_lock", Activator.CreateInstance(typeof(System.Threading.Lock)));
        Field(config, "_saved", new PpesConfig());
        Field(config, "_applied", new PpesConfig());
        Field(config, "<ModPath>k__BackingField", directory);
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

        var home = new Home();
        typeof(Home).GetProperty("Config", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(home, config);
        typeof(Home).GetProperty("Calendar", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(home, new SeasonalCalendar());
        typeof(Home).GetProperty("Snackbar", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(home, DispatchProxy.Create<ISnackbar, SilentSnackbar>());
        object? Invoke(string method, params object?[] args) => typeof(Home).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(home, args);
        T Get<T>(string field) => (T)typeof(Home).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(home)!;
        List<string> Pending() => (List<string>)typeof(Home).GetProperty("PendingRestart", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(home)!;
        Invoke("Discard");
        config.Update(c => { c.Mechanics.KollontayJam = 5; c.Mechanics.VengefulJam = -1; });
        Check(Pending().Count == 0, "Client jam settings alone do not require restart");
        config.Update(c => c.Mechanics.ToxinHydration = 12);
        Check(Pending().Any(s => s.Contains("Ritual") || s.Contains("ritual")), "Server Mechanics changes appear in restart warning");
        config.Update(c => { c.Mechanics.ToxinHydration = 10; c.SeasonalRotation.Mode = "weekly"; });
        Check(Pending().Count > 0, "Seasonal rotation change requires restart even during a break");
        Invoke("Discard");
        Field(home, "addRole", "bossTagilla");
        Field(home, "addEscort", "invalid");
        Invoke("AddSpawn");
        Check(Get<PpesConfig>("working").SpawnsOf("bigmap").Count == 0, "Invalid new-spawn escort does not mutate draft");
        Field(home, "addEscort", "1-3");
        Invoke("AddSpawn");
        Check(Get<PpesConfig>("working").SpawnsOf("bigmap").Single().Escort == "1-3", "Valid escort range is added");
        Invoke("SetEscort", "bossTagilla", "");
        var edit = Get<PpesConfig>("working").SpawnsOf("bigmap").Single();
        Check(edit.Escort is null && edit.Chance == 100, "Clearing escort preserves other spawn overrides");
        Field(home, "addEscort", "unfinished");
        Invoke("Discard");
        Check(Get<string>("addEscort") == "" && Get<string>("addRole") == "", "Discard also clears unfinished add-spawn controls");
        Invoke("SetEscort", "bossTagilla", "2");
        Invoke("SetEscort", "bossTagilla", "");
        Check(Get<PpesConfig>("working").SpawnsOf("bigmap").Count == 0, "Clearing the only override removes an empty spawn edit");
        var map = (LocationBase)RuntimeHelpers.GetUninitializedObject(typeof(LocationBase));
        var boss = (BossLocationSpawn)RuntimeHelpers.GetUninitializedObject(typeof(BossLocationSpawn));
        boss.BossName = "bossTagilla";
        boss.BossChance = 20;
        boss.BossZone = "ZoneTest";
        boss.BossEscortAmount = "2";
        map.BossLocationSpawn = [boss];
        map.AccessKeys = ["card"];
        map.AccessKeysPvE = ["card"];
        var location = new Location { Base = map };
        var maps = (LocationTable)RuntimeHelpers.GetUninitializedObject(typeof(LocationTable));
        Field(maps, "_locationDictionaryCache", new Dictionary<string, Location> { [maps.GetMappedKey("laboratory")] = location });
        var seasonal = (SeasonalEventConfig)RuntimeHelpers.GetUninitializedObject(typeof(SeasonalEventConfig));
        seasonal.EventBossSpawns = [];
        var previewer = new PreviewService(maps, new FixtureCloner(), new SeasonalBossRegistry(), new SeasonalCalendar(), seasonal, config);
        var selection = new PpesConfig { Seasonal = new() { ["halloween"] = "off" } };
        Check(previewer.Preview("laboratory", [], [], selection).NeedsKeycard == true, "Preview honors stock access requirements");
        map.AccessKeys = [];
        map.AccessKeysPvE = [];
        Check(previewer.Preview("laboratory", [], [], selection).NeedsKeycard == false, "Preview honors already-free access in map data");
        map.AccessKeys = ["card"];
        selection.Seasonal["halloween"] = "on";
        Check(previewer.Preview("laboratory", [], [], selection).NeedsKeycard == false, "Halloween free-Lab selection appears in preview");
        selection.Halloween.FreeLab = false;
        Check(previewer.Preview("laboratory", [], [], selection).NeedsKeycard == true, "Turning off free-Lab preference restores fixture requirement");
        var escorted = previewer.Preview("laboratory", [], [new SpawnEdit { Role = "bossTagilla", Escort = "4" }], selection);
        Check(escorted.Spawns.Single().Changed && escorted.Spawns.Single().EscortMax == 4, "Escort-only edits are marked as changed");
        Check(map.BossLocationSpawn.Single().BossEscortAmount == "2", "Preview leaves original escort data untouched");
        typeof(Home).GetProperty("Previewer", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(home, previewer);
        Field(home, "working", selection);
        Field(home, "previewMap", "laboratory");
        var previewProperty = typeof(Home).GetProperty("Preview", BindingFlags.NonPublic | BindingFlags.Instance)!;
        Check(((RaidPreview)previewProperty.GetValue(home)!).NeedsKeycard == true, "UI preview cache starts with current access requirement");
        selection.Halloween.FreeLab = true;
        Check(((RaidPreview)previewProperty.GetValue(home)!).NeedsKeycard == false, "Changing free-Lab preference invalidates UI preview cache");
        Console.WriteLine($"{checks} UI audit checks passed.");
    }
}

internal class FixtureCloner : ICloner
{
    public T? Clone<T>(T? value) => value is LocationBase map
        ? (T)(object)(map with { BossLocationSpawn = map.BossLocationSpawn?.Select(b => b with { }).ToList() ?? [] })
        : throw new InvalidOperationException("Unexpected fixture type");
}

public class SilentSnackbar : DispatchProxy
{
    protected override object? Invoke(MethodInfo? method, object?[]? args) => null;
}
