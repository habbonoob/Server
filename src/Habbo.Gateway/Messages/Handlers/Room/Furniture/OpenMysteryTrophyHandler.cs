using Habbo.Protocol.Messages.Incoming.Room.Furniture;

namespace Habbo.Gateway.Messages.Handlers.Room.Furniture;

/// <summary>OpenMysteryTrophyMessageEvent — open a mystery trophy box.</summary>
internal sealed class OpenMysteryTrophyHandler : IMessageHandler<OpenMysteryTrophyMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, OpenMysteryTrophyMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.OpenMysteryTrophyAsync(player.Id, message.ItemId, message.Inscription);
        return true;
    }
}
