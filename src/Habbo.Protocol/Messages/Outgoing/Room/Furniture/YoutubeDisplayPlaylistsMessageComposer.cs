using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Furniture;

/// <summary>YoutubeDisplayPlaylistsMessageComposer — the playlists a video TV offers, and the one chosen</summary>
[Message(ServerMessages.YoutubeDisplayPlaylistsMessageComposer)]
public sealed partial record YoutubeDisplayPlaylistsMessageComposer(int FurniId, IReadOnlyList<YoutubeDisplayPlaylistData> Playlists, string SelectedPlaylistId);
