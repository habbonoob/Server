using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>PlacePostItMessageEvent — stick a note from a pad to the wall</summary>
[Incoming(ClientMessages.PlacePostItMessageEvent)]
public sealed partial record PlacePostItMessageEvent(int ItemId, string Location);
