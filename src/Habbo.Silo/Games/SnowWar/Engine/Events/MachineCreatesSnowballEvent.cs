using Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;

namespace Habbo.Silo.Games.SnowWar.Engine.Events;

/// <summary>A machine makes a snowball.</summary>
internal sealed class MachineCreatesSnowballEvent(SnowballMachineGameObject machine) : SnowWarEvent
{
    public override void Apply(SnowWarStage stage)
    {
        machine.CreateSnowball();
    }

    public override SnowWarGameEventData ToWire() => new(SnowWarGameEventData.MachineCreatesSnowball, [machine.Id]);
}
