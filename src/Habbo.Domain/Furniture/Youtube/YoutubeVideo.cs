namespace Habbo.Domain.Furniture.Youtube;

/// <summary>A video of a playlist, by its YouTube id, and the part of it to play.</summary>
public sealed class YoutubeVideo
{
    public required string PlaylistId { get; set; }

    public int Position { get; set; }

    public required string VideoId { get; set; }

    public int StartAtSeconds { get; set; }

    /// <summary>0 plays it to its end.</summary>
    public int EndAtSeconds { get; set; }
}
