using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Domain.Moderation;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>CallForHelpFromIMMessageEvent — report instant messages.</summary>
internal sealed class CallForHelpFromIMHandler(IModerationService service) : IMessageHandler<CallForHelpFromIMMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, CallForHelpFromIMMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.ReportAsync(player.Id, new CallForHelpReport(ModerationIssueSources.InstantMessage, message.TopicId, message.Message, message.ReportedUserId, null, message.ChatLines.Select(l => new ReportedChatLine(l.UserId, l.Text)).ToList(), ReporterName: message.ReporterName, ReporterEmail: message.ReporterEmail));
        return true;
    }
}
