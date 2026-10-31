using System;
using System.Collections.Generic;
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
[BepInPlugin("com.zezinho.pastpresenteventselector", "zezinho-PastPresentEventSelector", "1.7.0")]
[BepInDependency("com.morebotsapi.tacticaltoaster", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("com.acidphantasm.botplacementsystem", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        var harmony = new Harmony("zezinho.ppes.client");
        try
        {
            MoreBotsWaveHeadroom.Enable(harmony, Logger);
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"MoreBotsAPI wave headroom not applied: {ex.Message}");
        }
        try
        {
            CustomFactionSupport.Enable(harmony);
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Custom faction support not applied: {ex.Message}");
        }
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

internal static class MoreBotsWaveHeadroom
{
    private static MethodInfo _isCustomRole;
    private static BepInEx.Logging.ManualLogSource _logger;
    private static int _baseCap;
    private static int _headroom;

    public static void Enable(Harmony harmony, BepInEx.Logging.ManualLogSource logger)
    {
        var manager = AccessTools.TypeByName("MoreBotsAPI.CustomWildSpawnTypeManager");
        var huntManager = AccessTools.TypeByName("MoreBotsAPI.Components.HuntManager");
        var abps = AccessTools.TypeByName("BotPlacementSystemClient.Plugin");
        _logger = logger;
        if (manager is not null)
            _isCustomRole = AccessTools.Method(manager, "IsCustomWildSpawnType", [typeof(int)])
                ?? throw new MissingMethodException(manager.FullName, "IsCustomWildSpawnType");

        // MoreBotsAPI 2.1.1 assumes every bot has an Id_spawn. Foreign and synthetic bots do not
        // always have one, and its global OnBotCreated callback otherwise throws before the other
        // subscribers get to finish their spawn work (upstream issue #5 / pull request #6).
        var huntCreated = huntManager is null ? null : AccessTools.Method(huntManager, "OnBotCreated");
        if (huntCreated is not null)
        {
            harmony.Patch(huntCreated,
                prefix: new HarmonyMethod(typeof(MoreBotsWaveHeadroom), nameof(BeforeMoreBotsHuntCreated))
                {
                    priority = Priority.First,
                });
            logger.LogInfo("MoreBotsAPI 2.1.1 foreign-bot spawn guard enabled");
        }

        if (abps is null) return;

        harmony.Patch(AccessTools.Method(typeof(BotsController), nameof(BotsController.SetSettings)),
            postfix: new HarmonyMethod(typeof(MoreBotsWaveHeadroom), nameof(AfterSetSettings))
            {
                priority = Priority.Last,
                after = ["com.acidphantasm.botplacementsystem"],
            });
        harmony.Patch(AccessTools.Method(typeof(BotOwner), "Create"),
            postfix: new HarmonyMethod(typeof(MoreBotsWaveHeadroom), nameof(AfterBotCreated)) { priority = Priority.Last });
        logger.LogInfo($"ABPS detected: adaptive custom-bot wave headroom enabled{(manager is null ? " for PPES factions" : " for PPES and MoreBotsAPI factions")}");
    }

    private static void AfterSetSettings(BotsController __instance)
    {
        _baseCap = __instance._maxCount;
        _headroom = 0;
    }

    private static void AfterBotCreated(Player player)
    {
        if (player is null || !player.IsAI || !IsCustom(player.Profile.Info.Settings.Role)) return;
        var controller = BotsController.FindBotControllerEditorOnly();
        if (controller is null || _baseCap <= 0) return;

        var customProfiles = controller.Bots.BotOwners
            .Where(owner => owner is not null && IsCustom(owner.Profile.Info.Settings.Role))
            .Select(owner => owner.ProfileId)
            .ToHashSet(StringComparer.Ordinal);
        customProfiles.Add(player.ProfileId);
        var required = customProfiles.Count;
        if (required <= _headroom) return;
        _headroom = required;
        controller._maxCount = _baseCap + _headroom;
        controller.BotSpawner?.SetMaxBots(controller._maxCount);
        controller.ZonesLeaveController?.SetMaxBots(controller._maxCount);
        _logger?.LogInfo($"Custom-bot headroom {_headroom}; effective bot cap {controller._maxCount}");
    }

    private static bool IsCustom(WildSpawnType role)
    {
        var id = (int)role;
        return id is >= 731001 and <= 731005 || _isCustomRole is not null && (bool)_isCustomRole.Invoke(null, [id]);
    }

    private static bool BeforeMoreBotsHuntCreated(BotOwner bot)
    {
        return bot?.SpawnProfileData?.SpawnParams?.Id_spawn is not null;
    }
}

internal static class CustomFactionSupport
{
    private static readonly (int Id, string ScavRole)[] Types =
    {
        (731001, "PpesCleanupCrew"),
        (731002, "PpesSmuggler"),
        (731003, "PpesPillager"),
        (731004, "PpesGus"),
        (731005, "PpesBasmach"),
    };
    private static bool _groupsAdded;

    public static void Enable(Harmony harmony)
    {
        var settingsField = AccessTools.Field(typeof(WildSpawnTypeExtension), "_spawnTypeSettings")
            ?? throw new MissingFieldException(nameof(WildSpawnTypeExtension), "_spawnTypeSettings");
        var settings = settingsField.GetValue(null) as IDictionary<WildSpawnType, WildSpawnTypeSettings>
            ?? throw new InvalidOperationException("WildSpawnType settings are unavailable");
        var difficultiesField = AccessTools.Field(typeof(BotInternalSettingsController), "ExcludedDifficulties")
            ?? throw new MissingFieldException(nameof(BotInternalSettingsController), "ExcludedDifficulties");
        var difficulties = difficultiesField.GetValue(null) as IDictionary<WildSpawnType, List<BotDifficulty>>
            ?? throw new InvalidOperationException("Bot difficulty exclusions are unavailable");

        foreach (var custom in Types)
        {
            var role = (WildSpawnType)custom.Id;
            if (!settings.ContainsKey(role))
            {
                var roleSettings = new WildSpawnTypeSettings(true, true, true, "ScavRole/" + custom.ScavRole)
                {
                    CountAsBossForStatistics = false,
                };
                settings.Add(role, roleSettings);
            }
            if (!difficulties.ContainsKey(role))
                difficulties.Add(role, new List<BotDifficulty> { BotDifficulty.easy, BotDifficulty.hard, BotDifficulty.impossible });
        }

        var init = AccessTools.Method(typeof(WildSpawnTypeExtension), "Init")
            ?? throw new MissingMethodException(nameof(WildSpawnTypeExtension), "Init");
        harmony.Patch(init, postfix: new HarmonyMethod(typeof(CustomFactionSupport), nameof(AfterInit)));
    }

    private static void AfterInit()
    {
        if (_groupsAdded) return;
        foreach (var custom in Types)
            WildSpawnTypeExtension.SubInitList(new List<WildSpawnType> { (WildSpawnType)custom.Id });
        _groupsAdded = true;
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
    private static AccessTools.FieldRef<BotMeleeWeaponData, BotOwner> _owner;

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
