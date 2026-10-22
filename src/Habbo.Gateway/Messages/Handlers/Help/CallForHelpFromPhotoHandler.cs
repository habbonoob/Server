using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Domain.Moderation;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>CallForHelpFromPhotoMessageEvent — report a photo.</summary>
internal sealed class CallForHelpFromPhotoHandler(IModerationService service) : IMessageHandler<CallForHelpFromPhotoMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, CallForHelpFromPhotoMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.ReportAsync(player.Id, new CallForHelpReport(ModerationIssueSources.Photo, message.TopicId, string.Empty, message.ReportedUserId, message.RoomId, [], PhotoId: message.PhotoId, ReporterName: message.ReporterName, ReporterEmail: message.ReporterEmail));
        return true;
    }
}
