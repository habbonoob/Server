using Habbo.Protocol.Messages.Incoming.Users;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>GiveStarGemToUserMessageEvent — Gives a user star gems.</summary>
internal sealed class GiveStarGemToUserHandler : IMessageHandler<GiveStarGemToUserMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GiveStarGemToUserMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.GiveStarGemsAsync(message.UserId, message.Amount);
        return true;
    }
}
