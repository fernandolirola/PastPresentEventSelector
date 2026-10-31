using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx;
using EFT;
using EFT.HealthSystem;
using HarmonyLib;
using SPT.Common.Http;

namespace PPES.Client;

/// <summary>
/// Client fixes for the event content. Each patch is a no-op unless the problem it fixes is present.
/// </summary>
// same GUID as the server mod: the Forge checks that both DLLs declare the registered one
[BepInPlugin("com.zezinho.pastpresenteventselector", "zezinho-PastPresentEventSelector", "1.6.1")]
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
        try
        {
            BossMisfire.Enable(harmony);
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Boss misfire setting not applied: {ex.Message}");
        }
    }
}

/// <summary>
/// Kollontay and Vengeful Zryachiy jam the player's weapon through the game's misfire effect. The page sets how many
/// seconds it lasts; the server hands the values out at /ppes/mechanics and they are re-read every half minute.
/// </summary>
internal static class BossMisfire
{
    // Kollontay applies 5 s; other callers (map triggers) pass their own time and are left alone
    private const float KollontayStock = 5f;
    private static float _kollontay = KollontayStock;
    private static float _vengeful = -1f;
    private static float _readAt = -100f;

    public static void Enable(Harmony harmony)
    {
        harmony.Patch(AccessTools.Method(typeof(ActiveHealthController), nameof(ActiveHealthController.AddMisfireEffect)),
            prefix: new HarmonyMethod(typeof(BossMisfire), nameof(AddMisfirePrefix)));
        harmony.Patch(AccessTools.Method(typeof(ActiveHealthController), nameof(ActiveHealthController.DoEventEffect)),
            prefix: new HarmonyMethod(typeof(BossMisfire), nameof(DoEventPrefix)));
    }

    private static void Refresh()
    {
        if (UnityEngine.Time.realtimeSinceStartup - _readAt < 30f) return;
        _readAt = UnityEngine.Time.realtimeSinceStartup;
        try
        {
            var json = RequestHandler.GetJson("/ppes/mechanics") ?? "";
            _kollontay = Read(json, "kollontayJam", _kollontay);
            _vengeful = Read(json, "vengefulJam", _vengeful);
        }
        catch
        {
            // server without the setting: keep the last value, stock at first
        }
    }

    private static float Read(string json, string key, float fallback)
    {
        var match = Regex.Match(json, "\"" + key + "\"\\s*:\\s*(-?[\\d.]+)");
        return match.Success && float.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : fallback;
    }

    private static bool AddMisfirePrefix(ref float? workTime)
    {
        Refresh();
        if (workTime != KollontayStock) return true;
        if (_kollontay <= 0f) return false;
        workTime = _kollontay;
        return true;
    }

    private static bool DoEventPrefix(ActiveHealthController __instance)
    {
        Refresh();
        if (_vengeful < 0f) return true;
        // keep the panic, replace the jam that never ends
        __instance.AddEffect<ActiveHealthController.PanicEffect>(EBodyPart.Head, 0f);
        if (_vengeful > 0f) __instance.AddMisfireEffect(_vengeful);
        return false;
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
