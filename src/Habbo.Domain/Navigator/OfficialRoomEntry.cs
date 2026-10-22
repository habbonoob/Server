using Habbo.Domain.Rooms;

namespace Habbo.Domain.Navigator;

/// <summary>An entry of the navigator's official rooms view: a room, a tag search, or a folder
/// holding other entries. Ad entries are shown one at a time, above the rest.</summary>
public sealed class OfficialRoomEntry
{
    public int Id { get; set; }

    /// <summary>The folder it sits in, if any.</summary>
    public int? FolderId { get; set; }

    public int SortOrder { get; set; }

    public OfficialRoomEntryType Type { get; set; }

    public string PopupCaption { get; set; } = string.Empty;

    public string PopupDescription { get; set; } = string.Empty;

    /// <summary>A room entry shows its room's details beside the picture.</summary>
    public bool ShowDetails { get; set; }

    public string PicText { get; set; } = string.Empty;

    public string PicRef { get; set; } = string.Empty;

    /// <summary>The tag a tag entry searches for.</summary>
    public string? Tag { get; set; }

    public int? RoomId { get; set; }
    public Room? Room { get; set; }

    /// <summary>A folder starts open.</summary>
    public bool Open { get; set; }

    /// <summary>One of the entries taking turns in the ad slot, rather than a listed one.</summary>
    public bool IsAd { get; set; }
}
