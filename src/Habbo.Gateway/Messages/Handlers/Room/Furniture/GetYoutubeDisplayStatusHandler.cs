using Habbo.Protocol.Messages.Incoming.Room.Furniture;

namespace Habbo.Gateway.Messages.Handlers.Room.Furniture;

/// <summary>GetYoutubeDisplayStatusMessageEvent — a video TV's playlists and video.</summary>
internal sealed class GetYoutubeDisplayStatusHandler : IMessageHandler<GetYoutubeDisplayStatusMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetYoutubeDisplayStatusMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SendYoutubeDisplayAsync(player.Id, message.ItemId);
        return true;
    }
}
