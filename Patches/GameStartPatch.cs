using System.Reflection;
using HarmonyLib;
using PPES.Services;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Common;

namespace PPES.Patches;

public sealed class GameStartPatch : AbstractPatch
{
    internal static TraderMessageService? Service;

    protected override MethodBase? GetTargetMethod() => AccessTools.Method(typeof(GameController), nameof(GameController.GameStart));

    [PatchPostfix]
    public static void Postfix(MongoId sessionId) => Service?.OnGameStart(sessionId);
}
