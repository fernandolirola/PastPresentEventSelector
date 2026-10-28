using System.Reflection;
using System.Text;
using System.Text.Json;
using HarmonyLib;
using PPES.Events;
using PPES.Models;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services.Commerce;

namespace PPES.Services;

/// <summary>
/// Sends one message from Fence at login listing the active events as rumors. It is sent again only when the
/// set of active events changes, tracked per profile in messages.json.
/// </summary>
[Injectable(InjectionType.Singleton)]
public sealed class TraderMessageService(ConfigService config, SeasonalCalendar calendar, MailSendService mail, ISptLogger<TraderMessageService> logger)
{
    private const string FenceId = "579dc571d53a0658a154fbec";
    private readonly Lock _lock = new();
    private Dictionary<string, string>? _sent;

    private string StatePath => Path.Combine(config.ModPath, "messages.json");

    public void OnGameStart(MongoId sessionId)
    {
        var settings = config.Saved;
        if (!settings.TraderMessage) return;
        try
        {
            var (seasonal, past) = ActiveNow();
            if (seasonal.Count == 0 && past.Count == 0) return;
            var fingerprint = string.Join(",", seasonal.Concat(past));

            lock (_lock)
            {
                _sent ??= LoadState();
                if (_sent.GetValueOrDefault(sessionId.ToString()) == fingerprint) return;
                _sent[sessionId.ToString()] = fingerprint;
                File.WriteAllText(StatePath, JsonSerializer.Serialize(_sent));
            }

            mail.SendDirectNpcMessageToPlayer(sessionId, FenceId, MessageType.NpcTraderMessage, Compose(seasonal, past, settings.Language == "pt"), null, 7 * 24 * 3600L);
        }
        catch (Exception ex)
        {
            logger.Warning($"{ConfigService.LogPrefix} Could not send the events message: {ex.Message}");
        }
    }

    /// <summary>Seasonal events as applied at startup; past events as the server runs them now (restart-bound ones from startup, raid ones live).</summary>
    public (List<string> Seasonal, List<string> Past) ActiveNow()
    {
        var now = DateTimeOffset.Now.DateTime;
        var applied = config.Applied;
        var saved = config.Saved;
        var seasonal = EventCatalog.Seasonal
            .Where(s => applied.ModeOf(s.Id) == SeasonalMode.On || (applied.ModeOf(s.Id) == SeasonalMode.Auto && calendar.InSeason(s.Id, now)))
            .Select(s => s.Id).ToList();
        var past = applied.Events.Where(id => EventCatalog.Past(id)?.NeedsRestart == true)
            .Concat(EventSchedule.RaidEvents(saved, now).Where(id => EventCatalog.Past(id)?.NeedsRestart == false));
        return (seasonal, ConfigService.NormalizeEvents(past));
    }

    private static string Compose(List<string> seasonal, List<string> past, bool pt)
    {
        var sb = new StringBuilder(pt ? "Escuta o que meu pessoal anda vendo por aí:\n" : "Listen to what my people are seeing out there:\n");
        foreach (var id in seasonal)
        {
            var (en, ptText) = EventDetails.SeasonalRumor(id);
            sb.Append("\n• ").Append(pt ? ptText : en);
        }
        foreach (var id in past)
        {
            var meta = EventDetails.Of(id);
            var line = pt ? meta.RumorPt : meta.RumorEn;
            if (!string.IsNullOrEmpty(line)) sb.Append("\n• ").Append(line);
        }
        return sb.ToString();
    }

    private Dictionary<string, string> LoadState()
    {
        try
        {
            return File.Exists(StatePath)
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(StatePath)) ?? new()
                : new();
        }
        catch
        {
            return new();
        }
    }
}

public sealed class GameStartPatch : AbstractPatch
{
    private static TraderMessageService? _service;

    public static void Init(TraderMessageService service) => _service = service;

    protected override MethodBase? GetTargetMethod() => AccessTools.Method(typeof(GameController), nameof(GameController.GameStart));

    [PatchPostfix]
    public static void Postfix(MongoId sessionId) => _service?.OnGameStart(sessionId);
}
