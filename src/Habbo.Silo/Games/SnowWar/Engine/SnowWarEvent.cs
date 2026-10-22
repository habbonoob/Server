using Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;

namespace Habbo.Silo.Games.SnowWar.Engine;

/// <summary>Something that happens in the game at a set subturn: applied to the server's stage and sent to the
/// clients to apply to theirs, in the same order, so both come out the same.</summary>
internal abstract class SnowWarEvent
{
    public abstract void Apply(SnowWarStage stage);

    public abstract SnowWarGameEventData ToWire();
}
