using System.Text;
using PPES.Events;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services.Commerce;

namespace PPES.Services;

/// <summary>
/// Sends one message from Fence at login listing the active events as rumors. It is sent again only when the
/// set of active events changes, tracked per profile in messages.json.
/// </summary>
[Injectable(InjectionType.Singleton)]
public sealed class TraderMessageService(ConfigService config, SeasonalCalendar calendar, EventSchedule schedule, MailSendService mail,
    ISptLogger<TraderMessageService> logger)
{
    private const string FenceId = "579dc571d53a0658a154fbec";
    private readonly Lock _lock = new();
    private Dictionary<string, string>? _sent;

    private StateFile<Dictionary<string, string>> State => new(Path.Combine(config.ModPath, "messages.json"));

    public void OnGameStart(MongoId sessionId)
    {
        var settings = config.Saved;
        if (!settings.TraderMessage) return;
        try
        {
            var seasonal = EventCatalog.Seasonal.Where(s => SeasonalState.Started(config, calendar).Active.Contains(s.Id)).ToList();
            var past = schedule.ActiveNow(DateTimeOffset.Now.DateTime).Select(EventCatalog.Past).OfType<PastEventDef>().ToList();
            if (seasonal.Count == 0 && past.Count == 0) return;
            var fingerprint = string.Join(",", seasonal.Select(s => s.Id).Concat(past.Select(e => e.Id)));

            lock (_lock)
            {
                _sent ??= State.Read();
                if (_sent.GetValueOrDefault(sessionId.ToString()) == fingerprint) return;
            }

            // recorded only once sent, so a failed send is tried again at the next login
            mail.SendDirectNpcMessageToPlayer(sessionId, FenceId, MessageType.NpcTraderMessage, Compose(seasonal, past, settings.Language == "pt"), null, 7 * 24 * 3600L);

            lock (_lock)
            {
                _sent![sessionId.ToString()] = fingerprint;
                State.Write(_sent);
            }
        }
        catch (Exception ex)
        {
            logger.Warning($"{ConfigService.LogPrefix} Could not send the events message: {ex.Message}");
        }
    }

    private static string Compose(List<SeasonalEventDef> seasonal, List<PastEventDef> past, bool pt)
    {
        var sb = new StringBuilder(pt ? "Escuta o que meu pessoal anda vendo por a\u00ed:\n" : "Listen to what my people are seeing out there:\n");
        var lines = seasonal.Select(s => pt ? s.RumorPt : s.RumorEn).Concat(past.Select(e => pt ? e.RumorPt : e.RumorEn));
        foreach (var line in lines.Where(l => !string.IsNullOrEmpty(l)))
            sb.Append("\n\u2022 ").Append(line);
        return sb.ToString();
    }
}
