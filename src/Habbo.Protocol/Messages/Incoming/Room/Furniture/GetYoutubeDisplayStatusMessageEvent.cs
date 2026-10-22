using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>GetYoutubeDisplayStatusMessageEvent — the playlists a video TV offers, and what it shows</summary>
[Incoming(ClientMessages.GetYoutubeDisplayStatusMessageEvent)]
public sealed partial record GetYoutubeDisplayStatusMessageEvent(int ItemId);
