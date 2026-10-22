using Habbo.Protocol.Messages.Incoming.Users;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>GetExtendedProfileMessageEvent — A user's profile.</summary>
internal sealed class GetExtendedProfileHandler : IMessageHandler<GetExtendedProfileMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetExtendedProfileMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.SendExtendedProfileAsync(message.UserId, message.OpenProfileWindow);
        return true;
    }
}
