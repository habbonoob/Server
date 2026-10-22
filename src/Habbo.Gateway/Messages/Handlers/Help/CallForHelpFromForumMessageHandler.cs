using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Domain.Moderation;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>CallForHelpFromForumMessageMessageEvent — report a forum message.</summary>
internal sealed class CallForHelpFromForumMessageHandler(IModerationService service) : IMessageHandler<CallForHelpFromForumMessageMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, CallForHelpFromForumMessageMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.ReportAsync(player.Id, new CallForHelpReport(ModerationIssueSources.Discussion, message.TopicId, message.Message, null, null, [], message.GroupId, message.ThreadId, message.MessageId, ReporterName: message.ReporterName, ReporterEmail: message.ReporterEmail));
        return true;
    }
}
