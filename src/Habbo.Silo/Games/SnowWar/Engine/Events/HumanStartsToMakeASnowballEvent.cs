using Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;

namespace Habbo.Silo.Games.SnowWar.Engine.Events;

/// <summary>A player bends down to make a snowball.</summary>
internal sealed class HumanStartsToMakeASnowballEvent(HumanGameObject human) : SnowWarEvent
{
    public override void Apply(SnowWarStage stage)
    {
        human.StartMakingSnowball();
    }

    public override SnowWarGameEventData ToWire() => new(SnowWarGameEventData.HumanStartsToMakeASnowball, [human.Id]);
}
