using Habbo.Protocol.Messages.Incoming.Room.Session;
using Habbo.Protocol.Messages.Outgoing.Room.Session;

namespace Habbo.Gateway.Messages.Handlers.Room.Session;

/// <summary>3981 Quit — leave the room the session is in. Habbo answers it like any other way out of a room: the game
/// that was being played is over (YouArePlayingGame false), then the room's connection is closed (CloseConnection).
/// habbo.com, 4 November 2026, leaving one's own room from the room tools: Quit, then UserUpdate, Achievement,
/// YouArePlayingGame {isPlaying: false}, CloseConnection.</summary>
internal sealed class QuitHandler : IMessageHandler<QuitMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, QuitMessageEvent message, CancellationToken ct)
    {
        await context.Session.LeaveRoomAsync();
        await context.SendAsync(new YouArePlayingGameMessageComposer(IsPlaying: false), ct);
        await context.SendAsync(new CloseConnectionMessageComposer(), ct);
        return true;
    }
}
