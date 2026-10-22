namespace Habbo.Domain.Camera;

/// <summary>A photo a player took with the camera: rendered by the server from the room as their client composed it,
/// kept as {Id}.png (and {Id}_small.png) in the photo store.</summary>
public sealed class Photo
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>The room it was taken in.</summary>
    public int RoomId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>When the player published it to their stories; null while it is not.</summary>
    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>How many photo furni were bought of it.</summary>
    public int Purchases { get; set; }
}
