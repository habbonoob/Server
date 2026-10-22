using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Furniture;

/// <summary>YoutubeControlVideoMessageComposer — play or pause what a TV shows</summary>
[Message(ServerMessages.YoutubeControlVideoMessageComposer)]
public sealed partial record YoutubeControlVideoMessageComposer(int FurniId, int CommandId);
