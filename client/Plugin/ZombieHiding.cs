using System;
using System.Text.RegularExpressions;
using HarmonyLib;
using SPT.Common.Http;

namespace PPES.Client;

internal static class ClientMechanics
{
    private static float _readAt = -100f;
    private static string _json = "";

    public static string Read()
    {
        var now = UnityEngine.Time.realtimeSinceStartup;
        if (now - _readAt < 30f) return _json;
        _readAt = now;
        try
        {
            _json = RequestHandler.GetJson("/ppes/mechanics") ?? _json;
        }
        catch
        {
            // Keep the last settings if the server is temporarily unavailable.
        }
        return _json;
    }

    public static bool BotsHideFromZombies(string json)
    {
        var match = Regex.Match(json, "\"botsHideFromZombies\"\\s*:\\s*(true|false)\\b");
        // Older servers do not send this setting; preserve their game behavior.
        return !match.Success || match.Groups[1].Value == "true";
    }
}

internal static class ZombieHiding
{
    public static void Enable(Harmony harmony)
    {
        var method = AccessTools.Method(typeof(HalloweenHideFromInfectedLayer), nameof(HalloweenHideFromInfectedLayer.ShallUseNow))
            ?? throw new MissingMethodException("HalloweenHideFromInfectedLayer.ShallUseNow");
        harmony.Patch(method, postfix: new HarmonyMethod(typeof(ZombieHiding), nameof(AfterShallUseNow)));
    }

    private static void AfterShallUseNow(ref bool __result)
    {
        if (__result && !ClientMechanics.BotsHideFromZombies(ClientMechanics.Read())) __result = false;
    }
}
