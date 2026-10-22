using Habbo.Protocol.Messages.Incoming.FriendList;
using Habbo.Silo.Abstractions.Friends;

namespace Habbo.Gateway.Messages.Handlers.FriendList;

/// <summary>GetMessengerHistoryEvent — older messages of a conversation.</summary>
internal sealed class GetMessengerHistoryHandler(IMessengerService service) : IMessageHandler<GetMessengerHistoryEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetMessengerHistoryEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendHistoryAsync(player.Id, message.ChatId, message.MessageId);
        return true;
    }
}
