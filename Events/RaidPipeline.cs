using PPES.Models;
using SPTarkov.Server.Core.Models.Spt.Config;

namespace PPES.Events;

/// <param name="Seasonal">The seasonal events and parts this raid runs with.</param>
/// <param name="Baseline">What SPT set the maps up with at startup; the same as <paramref name="Seasonal"/> for a real raid.</param>
/// <param name="Waves">Zombie wave settings, which apply from the next raid.</param>
/// <param name="Events">Past events whose raid edits apply.</param>
/// <param name="Edits">The user's spawn changes for this map.</param>
public sealed record RaidPlan(SeasonalState Seasonal, SeasonalState Baseline, HalloweenParts Waves, IReadOnlyCollection<string> Events, IReadOnlyList<SpawnEdit> Edits);

/// <summary>
/// Everything PPES does to a raid's copy of a map, in order. Raids and the page's preview both run this, so the
/// preview shows what the raid gets.
/// </summary>
internal static class RaidPipeline
{
    /// <param name="beforeEdits">Called once the events ran, before the user's spawn edits.</param>
    public static void Run(RaidContext ctx, RaidPlan plan, SeasonalEventConfig seasonalConfig, Action<string, Exception> failed, Action? beforeEdits = null)
    {
        Step("Seasonal content", () =>
        {
            SeasonalRaidContent.Apply(ctx, plan.Seasonal, plan.Baseline, seasonalConfig);
            if (plan.Seasonal.ZombiesOn && plan.Waves.ZombieWaves) SeasonalRaidContent.AddZombieWaves(ctx, plan.Waves);
        });
        foreach (var id in plan.Events)
        {
            if (EventCatalog.Past(id) is not { OnRaid: { } onRaid } def || def.MissingRequirement is not null) continue;
            Step(def.Name, () => onRaid(ctx));
        }
        beforeEdits?.Invoke();
        Step("Spawn edits", () => SpawnEdits.Apply(ctx, plan.Edits));

        void Step(string name, Action action)
        {
            try { action(); }
            catch (Exception ex) { failed(name, ex); }
        }
    }
}
