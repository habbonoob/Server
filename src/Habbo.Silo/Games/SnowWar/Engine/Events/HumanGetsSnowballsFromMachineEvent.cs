using Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;

namespace Habbo.Silo.Games.SnowWar.Engine.Events;

/// <summary>A player takes a snowball from a machine or a pile.</summary>
internal sealed class HumanGetsSnowballsFromMachineEvent(HumanGameObject human, SnowballGivingGameObject giver) : SnowWarEvent
{
    public override void Apply(SnowWarStage stage)
    {
        if (human.RemainingSnowballCapacity <= 0)
            return;
        var taken = giver.PickupSnowballs(1);
        if (taken > 0)
            human.TakeFromMachine(taken);
    }

    public override SnowWarGameEventData ToWire() => new(SnowWarGameEventData.HumanGetsSnowballsFromMachine, [human.Id, giver.Id]);
}
