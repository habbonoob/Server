namespace Habbo.Silo.Rooms;

/// <summary>Players let past a room's door for their next entry (a doorbell answered, a teleport arriving, staff
/// summoning them), each for a while only: a pass not used soon is gone, so it cannot open the door hours later.</summary>
public sealed class EntryPasses(TimeSpan lifetime)
{
    private readonly Dictionary<Guid, DateTimeOffset> _passes = [];

    public void Give(Guid playerId) => _passes[playerId] = DateTimeOffset.UtcNow + lifetime;

    /// <summary>Uses the player's pass: true when they had one still good.</summary>
    public bool Take(Guid playerId) => _passes.Remove(playerId, out var until) && until > DateTimeOffset.UtcNow;
}
