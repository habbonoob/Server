using Habbo.Protocol.Messages.Incoming.Users;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>GetExtendedProfileByNameMessageEvent — A user's profile, by name.</summary>
internal sealed class GetExtendedProfileByNameHandler : IMessageHandler<GetExtendedProfileByNameMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetExtendedProfileByNameMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.SendExtendedProfileByNameAsync(message.Name);
        return true;
    }
}
