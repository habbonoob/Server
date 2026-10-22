using Habbo.Protocol.Messages.Incoming.Room.Furniture;

namespace Habbo.Gateway.Messages.Handlers.Room.Furniture;

/// <summary>SpinWheelOfFortuneMessageEvent — spin the wheel.</summary>
internal sealed class SpinWheelOfFortuneHandler : IMessageHandler<SpinWheelOfFortuneMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SpinWheelOfFortuneMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SpinWheelAsync(player.Id, message.ItemId);
        return true;
    }
}
