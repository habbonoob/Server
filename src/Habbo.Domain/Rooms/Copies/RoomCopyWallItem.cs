namespace Habbo.Domain.Rooms.Copies;

/// <summary>A wall item of a copied room.</summary>
/// <param name="ClassId">Its type in the furnidata: the id of a wallitemtypes entry.</param>
/// <param name="ClassName">Its furnidata class name, when the copier knew it.</param>
/// <param name="Location">Where it hangs, as the client writes it (":w=3,2 l=10,40 r").</param>
/// <param name="State">What it shows (a lamp on or off, a post-it's colour).</param>
/// <param name="SourceId">Its id on the hotel it was copied from, which wired boxes pick it by.</param>
public sealed record RoomCopyWallItem(
    int ClassId,
    string? ClassName,
    string Location,
    string State,
    long SourceId = 0);
