using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>SetYoutubeDisplayPlaylistMessageEvent — choose a video TV's playlist</summary>
[Incoming(ClientMessages.SetYoutubeDisplayPlaylistMessageEvent)]
public sealed partial record SetYoutubeDisplayPlaylistMessageEvent(int ItemId, string PlaylistId);
