namespace Habbo.Domain.Furniture.Youtube;

/// <summary>A playlist the hotel's video TVs offer, with its videos in order.</summary>
public sealed class YoutubePlaylist
{
    public const int IdMaxLength = 64;

    public required string Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int OrderIndex { get; set; }

    public List<YoutubeVideo> Videos { get; set; } = [];
}
