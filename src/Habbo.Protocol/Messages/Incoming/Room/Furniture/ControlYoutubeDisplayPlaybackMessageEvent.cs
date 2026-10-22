using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>ControlYoutubeDisplayPlaybackMessageEvent — previous, next, pause or carry on</summary>
[Incoming(ClientMessages.ControlYoutubeDisplayPlaybackMessageEvent)]
public sealed partial record ControlYoutubeDisplayPlaybackMessageEvent(int ItemId, int Command);
