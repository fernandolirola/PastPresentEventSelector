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
[BepInPlugin("com.zezinho.pastpresenteventselector", "zezinho-PastPresentEventSelector", "1.6.0")]
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
/// Kollontay and Vengeful Zryachiy jam the player's weapon through the game's misfire effect. The page sets how long it
/// lasts in percent of stock; the server hands the value out at /ppes/mechanics and it is re-read every half minute.
/// </summary>
internal static class BossMisfire
{
    // stock Vengeful Zryachiy misfire has no timer; 100 percent keeps that, lower values become a timed jam of this length
    private const float TimedSecondsAtFull = 300f;
    private static int _percent = 100;
    private static float _readAt = -100f;

    public static void Enable(Harmony harmony)
    {
        harmony.Patch(AccessTools.Method(typeof(ActiveHealthController), nameof(ActiveHealthController.AddMisfireEffect)),
            prefix: new HarmonyMethod(typeof(BossMisfire), nameof(AddMisfirePrefix)));
        harmony.Patch(AccessTools.Method(typeof(ActiveHealthController), nameof(ActiveHealthController.DoEventEffect)),
            prefix: new HarmonyMethod(typeof(BossMisfire), nameof(DoEventPrefix)));
    }

    private static int Percent()
    {
        if (UnityEngine.Time.realtimeSinceStartup - _readAt < 30f) return _percent;
        _readAt = UnityEngine.Time.realtimeSinceStartup;
        try
        {
            var match = Regex.Match(RequestHandler.GetJson("/ppes/mechanics") ?? "", @"""misfirePercent""\s*:\s*(\d+)");
            if (match.Success) _percent = Math.Max(0, Math.Min(100, int.Parse(match.Groups[1].Value)));
        }
        catch
        {
            // server without the setting: keep the last value, stock at first
        }
        return _percent;
    }

    private static bool AddMisfirePrefix(ref float? workTime)
    {
        var percent = Percent();
        if (percent >= 100) return true;
        if (percent <= 0 || workTime is not { } time) return percent > 0;
        workTime = time * percent / 100f;
        return true;
    }

    private static bool DoEventPrefix(ActiveHealthController __instance)
    {
        var percent = Percent();
        if (percent >= 100) return true;
        // keep the panic, replace the permanent misfire
        __instance.AddEffect<ActiveHealthController.PanicEffect>(EBodyPart.Head, 0f);
        if (percent > 0) __instance.AddMisfireEffect(TimedSecondsAtFull * percent / 100f);
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
