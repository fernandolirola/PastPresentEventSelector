using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using EFT;
using HarmonyLib;

namespace PPES.Client;

/// <summary>
/// Client fixes for the event content. Each patch is a no-op unless the problem it fixes is present.
/// </summary>
// same GUID as the server mod: the Forge checks that both DLLs declare the registered one
[BepInPlugin("com.zezinho.pastpresenteventselector", "zezinho-PastPresentEventSelector", "1.5.1")]
public class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        var harmony = new Harmony("zezinho.ppes.client");
        try
        {
            InfectedMeleeFix.Enable(harmony);
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Infected melee fix not applied: {ex.Message}");
        }
    }
}

/// <summary>
/// SAIN's RunToEnemyUpdate prefix (Solarint/SAIN#278, fix proposed in Solarint/SAIN#328) skips the game's melee run
/// for every bot, so infected chase players but never swing. This prefix runs after SAIN's and lets the game's code run
/// again for bots on the zombie animator. HarmonyX runs every prefix, and __runOriginal is the final say.
/// </summary>
internal static class InfectedMeleeFix
{
    private static AccessTools.FieldRef<BotMeleeWeaponData, BotOwner>? _owner;

    public static void Enable(Harmony harmony)
    {
        var target = AccessTools.Method(typeof(BotMeleeWeaponData), "RunToEnemyUpdate") ?? throw new MissingMethodException("BotMeleeWeaponData.RunToEnemyUpdate");
        // the owner lives on the BotData base class, under a name that changes between game builds
        var field = AccessTools.FindIncludingBaseTypes(typeof(BotMeleeWeaponData), t => AccessTools.GetDeclaredFields(t).FirstOrDefault(f => f.FieldType == typeof(BotOwner)))
            ?? throw new MissingFieldException("BotMeleeWeaponData has no BotOwner field");
        _owner = AccessTools.FieldRefAccess<BotMeleeWeaponData, BotOwner>(field);
        harmony.Patch(target, prefix: new HarmonyMethod(typeof(InfectedMeleeFix), nameof(Prefix)) { priority = Priority.Last, after = ["me.sol.sain"] });
    }

    private static void Prefix(BotMeleeWeaponData __instance, ref bool __runOriginal)
    {
        if (__runOriginal || _owner is null) return;
        if (_owner(__instance)?.AIData?.UseZombieSimpleAnimator == true) __runOriginal = true;
    }
}
