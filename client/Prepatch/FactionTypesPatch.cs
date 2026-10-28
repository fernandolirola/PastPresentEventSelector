using System.Collections.Generic;
using Mono.Cecil;
using MoreBotsAPI;

namespace PPES.Prepatch;

/// <summary>
/// BepInEx preloader patcher: adds the PPES factions to the game's bot type list through MoreBotsAPI,
/// so the server can spawn them. The ids and names must match Factions/FactionCatalog.cs on the server.
/// </summary>
public static class FactionTypesPatch
{
    // MoreBotsAPI brain id for the PMC brain, the one Black Division also uses
    private const int PmcBrain = 9;

    private static readonly List<int> NormalOnly = new List<int> { 0, 2, 3 };

    public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

    public static void Patch(ref AssemblyDefinition assembly)
    {
        Register(assembly, 731001, "ppesCleanupCrew", "PpesCleanupCrew");
        Register(assembly, 731002, "ppesSmuggler", "PpesSmuggler");
        Register(assembly, 731003, "ppesPillager", "PpesPillager");

        // each faction groups only with its own members
        CustomWildSpawnTypeManager.AddSuitableGroup(new List<int> { 731001 });
        CustomWildSpawnTypeManager.AddSuitableGroup(new List<int> { 731002 });
        CustomWildSpawnTypeManager.AddSuitableGroup(new List<int> { 731003 });
    }

    private static void Register(AssemblyDefinition assembly, int id, string name, string scavRole)
    {
        // boss and follower at once lets one type lead its group and fill the escort, as Black Division does
        var type = new CustomWildSpawnType(id, name, scavRole, PmcBrain, true, true, false);
        type.SetCountAsBossForStatistics(false);
        type.SetShouldUseFenceNoBossAttack(false, false);
        type.SetExcludedDifficulties(NormalOnly);
        CustomWildSpawnTypeManager.RegisterWildSpawnType(type, assembly);
    }
}
