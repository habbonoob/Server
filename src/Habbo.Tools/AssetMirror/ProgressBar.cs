namespace Habbo.Tools.AssetMirror;

/// <summary>How far one kind of asset has got: how many files there are (which may grow, as badges add their pngs),
/// how many are done, how many of those were already there, how many the hotel does not have, how many failed, and
/// how many bytes came in.</summary>
public sealed class ProgressBar(string label)
{
    private int _total;
    private int _done;
    private int _skipped;
    private int _missing;
    private int _failed;
    private long _bytes;

    public string Label { get; } = label;
    public int Total => Volatile.Read(ref _total);
    public int Done => Volatile.Read(ref _done);
    public int Skipped => Volatile.Read(ref _skipped);
    public int Missing => Volatile.Read(ref _missing);
    public int Failed => Volatile.Read(ref _failed);
    public long Bytes => Interlocked.Read(ref _bytes);

    /// <summary>Whether the bar's work is all known and done.</summary>
    public bool Finished { get; set; }

    /// <summary>Set when there is nothing to fetch at all, and why.</summary>
    public string? Note { get; set; }

    public void AddToTotal(int count) => Interlocked.Add(ref _total, count);

    public void Fetched(long bytes)
    {
        Interlocked.Add(ref _bytes, bytes);
        Interlocked.Increment(ref _done);
    }

    public void AlreadyThere()
    {
        Interlocked.Increment(ref _skipped);
        Interlocked.Increment(ref _done);
    }

    /// <summary>The hotel does not have the file; counted when it was expected to.</summary>
    public void NotThere(bool expected)
    {
        if (expected)
            Interlocked.Increment(ref _missing);
        Interlocked.Increment(ref _done);
    }

    /// <summary>The file could not be fetched: the hotel or the network failed.</summary>
    public void Failure()
    {
        Interlocked.Increment(ref _failed);
        Interlocked.Increment(ref _done);
    }
}
