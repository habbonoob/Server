namespace Habbo.Domain.Furniture.Youtube;

/// <summary>What a video TV is showing: the playlist chosen for it, which of its videos, and whether it is paused.</summary>
public sealed class YoutubeDisplayState
{
    public int ItemId { get; set; }
    public UserItem? Item { get; set; }

    public required string PlaylistId { get; set; }

    public int Position { get; set; }

    public bool Paused { get; set; }
}
