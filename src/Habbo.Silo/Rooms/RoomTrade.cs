namespace Habbo.Silo.Rooms;

/// <summary>A trade between two avatars in the room. Both accept what is on the table; any change
/// takes the acceptances back; then both confirm, and it is done.</summary>
public sealed class RoomTrade(TradeSide first, TradeSide second)
{
    public TradeSide First { get; } = first;

    public TradeSide Second { get; } = second;

    /// <summary>Both accepted: now each confirms, and nothing may change.</summary>
    public bool Confirming { get; set; }

    public TradeSide SideOf(Guid playerId) => First.Entity.Player.Id == playerId ? First : Second;

    public TradeSide OtherThan(Guid playerId) => First.Entity.Player.Id == playerId ? Second : First;

    public IEnumerable<TradeSide> Sides => [First, Second];
}
