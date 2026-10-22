using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>484 FavouriteChangedComposer — one room's star turning on or off.</summary>
[Message(ServerMessages.FavouriteChangedComposer)]
public sealed partial record FavouriteChangedComposer(int RoomId, bool IsFavourite);
