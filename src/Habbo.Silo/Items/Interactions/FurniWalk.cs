using Habbo.Silo.Rooms;

namespace Habbo.Silo.Items.Interactions;

/// <summary>An avatar stepping onto an item: who, which item, and the room to act through.</summary>
public sealed record FurniWalk(RoomItemStore.PlacedItem Item, RoomEntityStore.RoomEntity User, IFurniRoom Room);
