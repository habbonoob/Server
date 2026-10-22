using System.Collections.Concurrent;

namespace Habbo.Silo.Collections;

/// <summary>Remembers keys for a while, to count or record something once per window rather than for every packet a
/// client sends: a view, a click, a look at a tab.</summary>
public sealed class RecentlySeen<TKey>(TimeSpan window) where TKey : notnull
{
    /// <summary>How many keys are remembered before the old ones are let go.</summary>
    private const int PruneAbove = 10_000;

    private readonly ConcurrentDictionary<TKey, DateTimeOffset> _seen = new();

    /// <summary>True the first time the key is seen in the window; false for every repeat within it.</summary>
    public bool FirstInWindow(TKey key)
    {
        var now = DateTimeOffset.UtcNow;
        if (_seen.Count > PruneAbove)
            foreach (var (old, at) in _seen)
                if (now - at >= window)
                    _seen.TryRemove(old, out _);

        while (true)
        {
            if (_seen.TryAdd(key, now))
                return true;
            if (!_seen.TryGetValue(key, out var last))
                continue;
            if (now - last < window)
                return false;
            if (_seen.TryUpdate(key, now, last))
                return true;
        }
    }
}
