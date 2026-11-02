using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PPES.Client;

internal sealed class RitualSpawnGate<TKey, TPrepared> where TKey : class where TPrepared : class
{
    private sealed class Entry
    {
        internal TPrepared Prepared;
    }

    private readonly ConditionalWeakTable<TKey, Entry> _pending = new ConditionalWeakTable<TKey, Entry>();

    internal bool IsReleased(TKey key) => _pending.TryGetValue(key, out _);

    internal bool TryTake(TKey key, out TPrepared prepared)
    {
        prepared = null;
        if (!_pending.TryGetValue(key, out var entry)) return false;
        _pending.Remove(key);
        prepared = entry.Prepared;
        return true;
    }

    internal async Task Run(TKey key, Func<Task<TPrepared>> prepare, Func<bool> stopped,
        Func<Task> release, Action<Exception> preparationFailed, Action<TPrepared> abandon)
    {
        TPrepared prepared = null;
        try { prepared = await prepare(); }
        catch (Exception ex) { preparationFailed(ex); }
        if (stopped())
        {
            if (prepared != null) abandon(prepared);
            return;
        }
        _pending.Add(key, new Entry { Prepared = prepared });
        try { await release(); }
        catch
        {
            if (TryTake(key, out var unused) && unused != null) abandon(unused);
            throw;
        }
    }
}
