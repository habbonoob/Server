using Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;

namespace Habbo.Silo.Games.SnowWar.Engine.Events;

/// <summary>A thrown snowball leaves the thrower's hand at snowball height.</summary>
internal sealed class CreateSnowballEvent(int snowballId, HumanGameObject human, int targetX, int targetY, int trajectory) : SnowWarEvent
{
    /// <summary>How high a snowball starts.</summary>
    private const int ThrowHeight = 3000;

    public override void Apply(SnowWarStage stage)
    {
        var snowball = new SnowballGameObject(snowballId);
        stage.AddObject(snowball);
        snowball.Initialize(human.CurrentLocation.X, human.CurrentLocation.Y, ThrowHeight, trajectory, targetX, targetY, human);
    }

    public override SnowWarGameEventData ToWire() => new(SnowWarGameEventData.CreateSnowball, [snowballId, human.Id, targetX, targetY, trajectory]);
}
