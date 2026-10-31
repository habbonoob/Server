using Habbo.Protocol.Messages.Incoming.FriendList;
using Habbo.Silo.Abstractions.Friends;
using Habbo.Silo.Abstractions.Habbicons;

namespace Habbo.Gateway.Messages.Handlers.FriendList;

/// <summary>SendHabbiconMessageEvent — a Habbicon the player owns, sent in the console in place of text.</summary>
internal sealed class SendHabbiconHandler(IMessengerService messengerService, IHabbiconService habbicons) : IMessageHandler<SendHabbiconMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SendHabbiconMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        if (await habbicons.OwnedNameAsync(player.Id, message.HabbiconId) is { } name)
            await messengerService.SendHabbiconAsync(player.Id, message.ChatId, message.HabbiconId, name, message.ConfirmationId);
        return true;
    }
}
