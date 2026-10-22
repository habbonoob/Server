using Habbo.Protocol.Messages.Incoming.Camera;
using Habbo.Protocol.Messages.Outgoing.Camera;

namespace Habbo.Gateway.Messages.Handlers.Camera;

/// <summary>PhotoCompetitionMessageEvent — enter the photo just taken in the photo competition. The hotel runs none
/// (camera.competition.enabled is false, so the client offers no button), so an entry is refused.</summary>
internal sealed class PhotoCompetitionHandler : IMessageHandler<PhotoCompetitionMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, PhotoCompetitionMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is null)
            return false;

        await context.SendAsync(new CompetitionStatusMessageComposer(false, string.Empty), ct);
        return true;
    }
}
