using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>FavoriteMembershipUpdateMessageComposer — a user in the room picked another favourite group</summary>
[Message(ServerMessages.FavoriteMembershipUpdateMessageComposer)]
public sealed partial record FavoriteMembershipUpdateMessageComposer(int RoomIndex, int HabboGroupId, int Status, string HabboGroupName);
