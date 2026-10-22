using Habbo.Protocol.Messages.Incoming.Room.Furniture;

namespace Habbo.Gateway.Messages.Handlers.Room.Furniture;

/// <summary>SetYoutubeDisplayPlaylistMessageEvent — choose a video TV's playlist.</summary>
internal sealed class SetYoutubeDisplayPlaylistHandler : IMessageHandler<SetYoutubeDisplayPlaylistMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SetYoutubeDisplayPlaylistMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SetYoutubePlaylistAsync(player.Id, message.ItemId, message.PlaylistId);
        return true;
    }
}
