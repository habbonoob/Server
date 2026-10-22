using Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;

namespace Habbo.Silo.Games.SnowWar.Engine.Events;

/// <summary>A player left the game: they are taken off the field.</summary>
internal sealed class HumanLeftGameEvent(HumanGameObject human) : SnowWarEvent
{
    public override void Apply(SnowWarStage stage)
    {
        stage.PutOnDeleteList(human);
        human.OnRemove();
    }

    public override SnowWarGameEventData ToWire() => new(SnowWarGameEventData.HumanLeftGame, [human.Id]);
}
