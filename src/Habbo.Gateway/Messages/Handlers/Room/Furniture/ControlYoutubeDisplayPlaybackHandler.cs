using Habbo.Protocol.Messages.Incoming.Room.Furniture;

namespace Habbo.Gateway.Messages.Handlers.Room.Furniture;

/// <summary>ControlYoutubeDisplayPlaybackMessageEvent — control a video TV.</summary>
internal sealed class ControlYoutubeDisplayPlaybackHandler : IMessageHandler<ControlYoutubeDisplayPlaybackMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ControlYoutubeDisplayPlaybackMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.ControlYoutubeAsync(player.Id, message.ItemId, message.Command);
        return true;
    }
}
