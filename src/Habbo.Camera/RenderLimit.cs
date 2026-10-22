using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Habbo.Camera;

/// <summary>How many renders a player has had in the last minute: rendering is the heaviest thing a client can ask of
/// the server, so it is counted (CameraOptions.RendersPerMinute).</summary>
public sealed class RenderLimit(IOptions<CameraOptions> options, TimeProvider time)
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private readonly ConcurrentDictionary<Guid, Queue<DateTimeOffset>> _renders = new();

    /// <summary>Counts a render of the player's, unless they have had their minute's.</summary>
    public bool TryTake(Guid playerId)
    {
        var now = time.GetUtcNow();
        var renders = _renders.GetOrAdd(playerId, _ => new Queue<DateTimeOffset>());
        lock (renders)
        {
            while (renders.Count > 0 && now - renders.Peek() >= Window)
                renders.Dequeue();
            if (renders.Count >= options.Value.RendersPerMinute)
                return false;
            renders.Enqueue(now);
            return true;
        }
    }
}
