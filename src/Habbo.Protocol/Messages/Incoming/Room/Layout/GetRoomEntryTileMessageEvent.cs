using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Layout;

/// <summary>GetRoomEntryTileMessageEvent</summary>
[Incoming(ClientMessages.GetRoomEntryTileMessageEvent)]
public sealed partial record GetRoomEntryTileMessageEvent;
