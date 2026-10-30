using System.Reflection;
using System.Runtime.CompilerServices;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace PPES.Events;

/// <summary>
/// Vultify's Blackout mod rolls each Labs raid on its server and its client asks for the result once the raid
/// loads. Setting that result to blackout turns on its darkness, gate-only extracts, keypad codes and arsenal key.
/// Found by reflection so PPES has no reference to it.
/// </summary>
public static class VultifyBlackout
{
    private const string ControllerType = "BlackoutServer.BlackoutSpawnController";

    private static object? _controller;
    private static PropertyInfo? _state;
    private static (string Tpl, string SpawnId, Vector3 Position)? _key;

    public static bool Loaded => _controller is not null;

    internal static void Find<T>(IServiceProvider services, ISptLogger<T> logger)
    {
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(ControllerType, false))
            .FirstOrDefault(t => t is not null);
        if (type is null) return;

        try
        {
            _state = type.GetProperty("CurrentRaidBlackout");
            _key = ReadKey(type);
            if (_state?.SetMethod is null) throw new MissingMemberException(ControllerType, "CurrentRaidBlackout");
            _controller = services.GetService(type) ?? throw new InvalidOperationException("controller not registered");
            logger.Info($"{Services.ConfigService.LogPrefix} Vultify's Blackout found: the Blackout event turns the Lab dark");
        }
        catch (Exception ex)
        {
            _controller = null;
            logger.Warning($"{Services.ConfigService.LogPrefix} Vultify's Blackout found but this version can't be linked: {ex.Message}");
        }
    }

    // read from their mod so a moved desk or a new key id follows their updates
    private static (string, string, Vector3)? ReadKey(Type type)
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        if (type.GetField("ArsenalKeyTpl", flags)?.GetRawConstantValue() is not string tpl
            || type.GetField("ArsenalKeySpawnId", flags)?.GetRawConstantValue() is not string spawnId
            || type.GetField("DeskPoint", flags)?.GetValue(null) is not ITuple { Length: 3 } desk)
            return null;
        return (tpl, spawnId, new Vector3 { X = Convert.ToSingle(desk[0]), Y = Convert.ToSingle(desk[1]), Z = Convert.ToSingle(desk[2]) });
    }

    /// <summary>Makes this Labs raid a blackout raid.</summary>
    public static void Force(RaidContext ctx)
    {
        if (_controller is null || _state is null) return;
        if (_state.GetValue(_controller) is true) return;

        _state.SetValue(_controller, true);
        // their key goes into the loot only when their own roll said blackout, and this raid's loot is already made
        if (_key is { } key && ctx.Location.Loot is { } loot && !loot.Any(s => s.Id == key.SpawnId))
            ctx.Location.Loot = [.. loot, ArsenalKey(key.Tpl, key.SpawnId, key.Position)];
    }

    private static SpawnpointTemplate ArsenalKey(string tpl, string spawnId, Vector3 position)
    {
        var itemId = new MongoId();
        return new SpawnpointTemplate
        {
            Id = spawnId,
            IsContainer = false,
            UseGravity = false,
            RandomRotation = false,
            Position = position,
            Rotation = new Vector3 { X = 0, Y = 0, Z = 0 },
            IsAlwaysSpawn = true,
            IsGroupPosition = false,
            GroupPositions = [],
            Root = itemId.ToString(),
            Items = [new SptLootItem { Id = itemId, Template = new MongoId(tpl), Upd = new Upd { StackObjectsCount = 1 } }],
        };
    }
}
