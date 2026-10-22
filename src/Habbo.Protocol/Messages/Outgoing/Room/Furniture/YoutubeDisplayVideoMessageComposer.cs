using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Furniture;

/// <summary>YoutubeDisplayVideoMessageComposer — the video a TV shows</summary>
[Message(ServerMessages.YoutubeDisplayVideoMessageComposer)]
public sealed partial record YoutubeDisplayVideoMessageComposer(int FurniId, string VideoId, int StartAtSeconds, int EndAtSeconds, int State);
