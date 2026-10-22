using Habbo.Protocol.Messages.Incoming.Users;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>GetMOTDMessageEvent — The messages of the day.</summary>
internal sealed class GetMotdHandler : IMessageHandler<GetMOTDMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetMOTDMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.SendMessagesOfTheDayAsync();
        return true;
    }
}
