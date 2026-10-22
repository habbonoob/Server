using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Domain.Moderation;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>CallForHelpFromSelfieMessageEvent — report a selfie.</summary>
internal sealed class CallForHelpFromSelfieHandler(IModerationService service) : IMessageHandler<CallForHelpFromSelfieMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, CallForHelpFromSelfieMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.ReportAsync(player.Id, new CallForHelpReport(ModerationIssueSources.Selfie, message.TopicId, message.Message, message.ReportedUserId, message.RoomId, [], PhotoId: message.SelfieId));
        return true;
    }
}
