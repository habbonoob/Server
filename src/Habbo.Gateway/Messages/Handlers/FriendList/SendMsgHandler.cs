using Habbo.Protocol.Messages.Incoming.FriendList;
using Habbo.Silo.Abstractions.Friends;

namespace Habbo.Gateway.Messages.Handlers.FriendList;

/// <summary>SendMsgMessageEvent — a console message to a friend or a group chat.</summary>
internal sealed class SendMsgHandler(IMessengerService messenger) : IMessageHandler<SendMsgMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SendMsgMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await messenger.SendMessageAsync(player.Id, message.FriendId, message.Message, message.ConfirmationId);
        return true;
    }
}
