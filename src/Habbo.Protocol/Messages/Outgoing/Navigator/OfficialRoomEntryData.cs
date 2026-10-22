using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>OfficialRoomEntryData</summary>
[WireStruct]
public sealed partial record OfficialRoomEntryData(int Index, string PopupCaption, string PopupDescription, int ShowDetails, string PicText, string PicRef, int FolderId, int UserCount, int Type, [property: WireWhen("IsTag")] string? Tag, [property: WireWhen("IsRoom")] GuestRoomData? Room, [property: WireWhen("IsFolder")] bool? Open)
{
    // what an entry is: a folder of others, a tag search, or a room
    public const int FolderType = 0;
    public const int TagType = 1;
    public const int RoomType = 2;

    // the details flag is read as an int, 1 meaning shown
    public const int DetailsHidden = 0;
    public const int DetailsShown = 1;

    /// <summary>The folder id of an entry at the top level.</summary>
    public const int NoFolder = 0;

    public bool IsTag => Type == TagType;
    public bool IsRoom => Type == RoomType;
    public bool IsFolder => Type is not (TagType or RoomType);
}
