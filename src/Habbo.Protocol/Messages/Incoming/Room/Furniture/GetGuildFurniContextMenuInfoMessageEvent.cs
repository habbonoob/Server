using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>GetGuildFurniContextMenuInfoMessageEvent</summary>
[Incoming(ClientMessages.GetGuildFurniContextMenuInfoMessageEvent)]
public sealed partial record GetGuildFurniContextMenuInfoMessageEvent(int ItemId, int GroupId);
