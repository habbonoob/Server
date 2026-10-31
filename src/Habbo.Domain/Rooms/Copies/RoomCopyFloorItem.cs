namespace Habbo.Domain.Rooms.Copies;

/// <summary>A floor item of a copied room.</summary>
/// <param name="ClassId">Its type in the furnidata: the id of a roomitemtypes entry.</param>
/// <param name="ClassName">Its furnidata class name, when the copier knew it.</param>
/// <param name="SourceId">Its id on the hotel it was copied from, which wired boxes pick it by.</param>
/// <param name="Wired">A wired box's settings, when it is one and they could be read.</param>
/// <param name="Extra">The number the hotel sends everyone with each floor item (format 4). For a teleport it is the
/// <see cref="SourceId"/> of the teleport it is linked to.</param>
public sealed record RoomCopyFloorItem(
    int ClassId,
    string? ClassName,
    int X,
    int Y,
    double Z,
    int Direction,
    RoomCopyItemData Data,
    long SourceId = 0,
    RoomCopyWired? Wired = null,
    long Extra = 0);
