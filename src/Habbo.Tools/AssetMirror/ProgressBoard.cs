using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Habbo.Tools.AssetMirror;

/// <summary>Draws a progress bar per kind of asset, redrawn in place a few times a second. When the output is not a
/// console (piped to a file), it writes a line per bar now and then instead.</summary>
public sealed class ProgressBoard : IAsyncDisposable
{
    private const int BarWidth = 30;
    private const int LabelWidth = 10;
    private static readonly TimeSpan ConsoleInterval = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan PipedInterval = TimeSpan.FromSeconds(10);

    private readonly List<ProgressBar> _bars = [];
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly CancellationTokenSource _stop = new();
    private readonly bool _interactive = !Console.IsOutputRedirected;
    private Task? _drawing;
    private int _top = -1;
    private int _drawnLines;

    public ProgressBar Add(string label)
    {
        var bar = new ProgressBar(label);
        lock (_bars)
            _bars.Add(bar);
        return bar;
    }

    public void Start()
    {
        if (_interactive)
            Console.CursorVisible = false;
        _drawing = DrawLoopAsync(_stop.Token);
    }

    private async Task DrawLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(_interactive ? ConsoleInterval : PipedInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
                Draw();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void Draw()
    {
        List<ProgressBar> bars;
        lock (_bars)
            bars = [.. _bars];
        var lines = bars.Select(Line).ToList();

        if (!_interactive)
        {
            foreach (var line in lines)
                Console.WriteLine(line.TrimEnd());
            return;
        }

        if (_top < 0)
            _top = Console.CursorTop;

        // the console may have scrolled under us: start where the board fits
        if (_top + lines.Count >= Console.BufferHeight)
            _top = Math.Max(0, Console.BufferHeight - lines.Count - 1);

        var width = Math.Max(20, Console.WindowWidth - 1);
        Console.SetCursorPosition(0, _top);
        foreach (var line in lines)
            Console.WriteLine(line.Length > width ? line[..width] : line.PadRight(width));
        _drawnLines = lines.Count;
    }

    private string Line(ProgressBar bar)
    {
        var label = bar.Label.PadRight(LabelWidth);
        if (bar.Note is { } note)
            return $"{label} {note}";

        var total = bar.Total;
        var done = bar.Done;
        var fraction = total == 0 ? (bar.Finished ? 1 : 0) : (double)done / total;
        var filled = (int)Math.Round(fraction * BarWidth);
        var text = new StringBuilder(label).Append(" [")
            .Append('#', filled).Append('-', BarWidth - filled).Append("] ")
            .Append(string.Create(CultureInfo.InvariantCulture, $"{done,6}/{total,-6} {fraction,4:P0}"));

        if (bar.Skipped > 0)
            text.Append(CultureInfo.InvariantCulture, $"  {bar.Skipped} there");
        if (bar.Missing > 0)
            text.Append(CultureInfo.InvariantCulture, $"  {bar.Missing} missing");
        if (bar.Failed > 0)
            text.Append(CultureInfo.InvariantCulture, $"  {bar.Failed} failed");
        var seconds = _clock.Elapsed.TotalSeconds;
        if (bar.Bytes > 0 && seconds > 0)
            text.Append("  ").Append(Size(bar.Bytes)).Append(" (").Append(Size((long)(bar.Bytes / seconds))).Append("/s)");
        if (bar.Finished)
            text.Append("  done");
        return text.ToString();
    }

    private static string Size(long bytes) => bytes switch
    {
        >= 1L << 30 => string.Create(CultureInfo.InvariantCulture, $"{bytes / (double)(1L << 30):0.0} GB"),
        >= 1L << 20 => string.Create(CultureInfo.InvariantCulture, $"{bytes / (double)(1L << 20):0.0} MB"),
        >= 1L << 10 => string.Create(CultureInfo.InvariantCulture, $"{bytes / (double)(1L << 10):0} KB"),
        _ => string.Create(CultureInfo.InvariantCulture, $"{bytes} B"),
    };

    /// <summary>Stops redrawing, draws the final state once more and leaves the cursor under the board.</summary>
    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        if (_drawing is not null)
            await _drawing;
        Draw();
        if (_interactive)
        {
            Console.SetCursorPosition(0, Math.Min(Console.BufferHeight - 1, _top + _drawnLines));
            Console.CursorVisible = true;
        }

        _stop.Dispose();
    }
}
