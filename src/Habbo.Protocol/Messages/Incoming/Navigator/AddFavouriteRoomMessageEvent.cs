using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>3089 AddFavouriteRoom.</summary>
[Incoming(ClientMessages.AddFavouriteRoomMessageEvent)]
public sealed partial record AddFavouriteRoomMessageEvent(int RoomId);
