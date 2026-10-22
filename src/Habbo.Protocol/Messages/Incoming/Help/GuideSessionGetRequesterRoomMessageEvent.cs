using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>GuideSessionGetRequesterRoomMessageEvent — go to the requester's room</summary>
[Incoming(ClientMessages.GuideSessionGetRequesterRoomMessageEvent)]
public sealed partial record GuideSessionGetRequesterRoomMessageEvent;
