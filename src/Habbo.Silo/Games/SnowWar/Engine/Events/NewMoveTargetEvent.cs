using Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;

namespace Habbo.Silo.Games.SnowWar.Engine.Events;

/// <summary>A player was told where to walk.</summary>
internal sealed class NewMoveTargetEvent(HumanGameObject human, int x, int y) : SnowWarEvent
{
    public override void Apply(SnowWarStage stage)
    {
        human.ChangeMoveTarget(x, y);
    }

    public override SnowWarGameEventData ToWire() => new(SnowWarGameEventData.NewMoveTarget, [human.Id, x, y]);
}
