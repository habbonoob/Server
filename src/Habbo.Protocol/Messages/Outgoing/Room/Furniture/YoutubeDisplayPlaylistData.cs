using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Furniture;

/// <summary>YoutubeDisplayPlaylistData — a playlist a video TV offers</summary>
[WireStruct]
public sealed partial record YoutubeDisplayPlaylistData(string PlaylistId, string Title, string Description);
