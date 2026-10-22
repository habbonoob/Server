using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Domain.Moderation;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>CallForHelpFromForumThreadMessageEvent — report a forum thread.</summary>
internal sealed class CallForHelpFromForumThreadHandler(IModerationService service) : IMessageHandler<CallForHelpFromForumThreadMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, CallForHelpFromForumThreadMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.ReportAsync(player.Id, new CallForHelpReport(ModerationIssueSources.Discussion, message.TopicId, message.Message, null, null, [], message.GroupId, message.ThreadId, ReporterName: message.ReporterName, ReporterEmail: message.ReporterEmail));
        return true;
    }
}
