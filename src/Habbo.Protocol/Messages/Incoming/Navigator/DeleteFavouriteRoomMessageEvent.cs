using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>3512 DeleteFavouriteRoom.</summary>
[Incoming(ClientMessages.DeleteFavouriteRoomMessageEvent)]
public sealed partial record DeleteFavouriteRoomMessageEvent(int RoomId);
