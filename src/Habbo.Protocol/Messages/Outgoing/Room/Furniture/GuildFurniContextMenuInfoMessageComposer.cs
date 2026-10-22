using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Furniture;

/// <summary>GuildFurniContextMenuInfoMessageComposer</summary>
[Message(ServerMessages.GuildFurniContextMenuInfoMessageComposer)]
public sealed partial record GuildFurniContextMenuInfoMessageComposer(int ItemId, int GroupId, string GroupName, int HomeRoomId, bool IsMember, bool HasReadableForum);
