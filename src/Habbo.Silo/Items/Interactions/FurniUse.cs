using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Items.Interactions;

/// <summary>One use of a placed item: who, what they asked for, and the room to act through.</summary>
public sealed record FurniUse(
    RoomItemStore.PlacedItem Item,
    RoomEntityStore.RoomEntity User,
    FurniAction Action,
    int Param,
    IFurniRoom Room);
