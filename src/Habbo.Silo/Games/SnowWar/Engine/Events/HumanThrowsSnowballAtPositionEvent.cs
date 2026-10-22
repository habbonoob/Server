using Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;

namespace Habbo.Silo.Games.SnowWar.Engine.Events;

/// <summary>A player throws at a point; the snowball itself comes in a CreateSnowballEvent.</summary>
internal sealed class HumanThrowsSnowballAtPositionEvent(HumanGameObject human, int x, int y, int trajectory) : SnowWarEvent
{
    public override void Apply(SnowWarStage stage)
    {
        human.ThrowSnowball(x, y);
        human.StartThrowTimer();
    }

    public override SnowWarGameEventData ToWire() => new(SnowWarGameEventData.HumanThrowsSnowballAtPosition, [human.Id, x, y, trajectory]);
}
