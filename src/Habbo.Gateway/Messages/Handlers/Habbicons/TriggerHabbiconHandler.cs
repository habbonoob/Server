using Habbo.Protocol.Messages.Incoming.Habbicons;
using Habbo.Silo.Abstractions.Habbicons;

namespace Habbo.Gateway.Messages.Handlers.Habbicons;

/// <summary>TriggerHabbiconMessageEvent — show an owned Habbicon over the avatar, to the room.</summary>
internal sealed class TriggerHabbiconHandler(IHabbiconService habbicons) : IMessageHandler<TriggerHabbiconMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, TriggerHabbiconMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        if (await habbicons.UseAsync(player.Id, message.HabbiconId))
            await room.UseHabbiconAsync(player.Id, message.HabbiconId);
        return true;
    }
}
