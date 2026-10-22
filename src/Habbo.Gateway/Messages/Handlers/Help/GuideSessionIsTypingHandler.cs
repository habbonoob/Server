using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Silo.Abstractions.Moderation;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>GuideSessionIsTypingMessageEvent — typing in the session.</summary>
internal sealed class GuideSessionIsTypingHandler(IGrainFactory service) : IMessageHandler<GuideSessionIsTypingMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GuideSessionIsTypingMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.GetGrain<IGuideDeskGrain>(0).TypingAsync(player, message.IsTyping);
        return true;
    }
}
