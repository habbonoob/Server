using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>GuideSessionRequesterRoomMessageComposer — the requester's room, 0 when they are in none</summary>
[Message(ServerMessages.GuideSessionRequesterRoomMessageComposer)]
public sealed partial record GuideSessionRequesterRoomMessageComposer(int RequesterRoomId);
