using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using SPT.Common.Http;

namespace PPES.Client;

internal static class ClientMechanics
{
    private static float _readAt = -100f;
    private static volatile string _json = "";
    private static int _fetching;

    /// <summary>
    /// Only the first read waits for the server; later refreshes run in the background so AI and hit code on the
    /// game thread never stall on a request.
    /// </summary>
    public static string Read()
    {
        var now = UnityEngine.Time.realtimeSinceStartup;
        if (now - _readAt < 30f) return _json;
        _readAt = now;
        if (_json.Length == 0) Fetch();
        else if (Interlocked.CompareExchange(ref _fetching, 1, 0) == 0)
            Task.Run(() =>
            {
                try { Fetch(); }
                finally { Interlocked.Exchange(ref _fetching, 0); }
            });
        return _json;
    }

    private static void Fetch()
    {
        try
        {
            _json = RequestHandler.GetJson("/ppes/mechanics") ?? _json;
        }
        catch
        {
            // Keep the last settings if the server is temporarily unavailable.
        }
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
