using Habbo.Protocol.Messages.Incoming.Users;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>GetRelationshipStatusInfoMessageEvent — A user's friends by relationship.</summary>
internal sealed class GetRelationshipStatusInfoHandler : IMessageHandler<GetRelationshipStatusInfoMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetRelationshipStatusInfoMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.SendRelationshipsAsync(message.UserId);
        return true;
    }
}
