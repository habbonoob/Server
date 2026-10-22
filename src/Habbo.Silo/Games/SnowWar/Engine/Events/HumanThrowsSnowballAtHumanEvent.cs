using Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;

namespace Habbo.Silo.Games.SnowWar.Engine.Events;

/// <summary>A player throws at another; the snowball itself comes in a CreateSnowballEvent.</summary>
internal sealed class HumanThrowsSnowballAtHumanEvent(HumanGameObject human, HumanGameObject target, int trajectory) : SnowWarEvent
{
    public override void Apply(SnowWarStage stage)
    {
        human.ThrowSnowball(target.CurrentLocation.X, target.CurrentLocation.Y);
        human.StartThrowTimer();
    }

    public override SnowWarGameEventData ToWire() => new(SnowWarGameEventData.HumanThrowsSnowballAtHuman, [human.Id, target.Id, trajectory]);
}
