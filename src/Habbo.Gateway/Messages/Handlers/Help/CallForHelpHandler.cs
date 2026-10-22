using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Domain.Moderation;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>CallForHelpMessageEvent — report a user or a room.</summary>
internal sealed class CallForHelpHandler(IModerationService service) : IMessageHandler<CallForHelpMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, CallForHelpMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.ReportAsync(player.Id, new CallForHelpReport(ModerationIssueSources.Normal, message.TopicId, message.Message, message.ReportedUserId, message.RoomId, message.ChatLines.Select(l => new ReportedChatLine(l.UserId, l.Text)).ToList(), ReporterName: message.ReporterName, ReporterEmail: message.ReporterEmail));
        return true;
    }
}
