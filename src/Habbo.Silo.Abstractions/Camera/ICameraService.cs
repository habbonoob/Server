using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Camera;

/// <summary>The camera's photos: recorded as they are taken, bought as photo furni, published to the player's
/// stories. Each acts on the player's latest photo, the one the camera's purchase dialog shows.</summary>
public interface ICameraService
{
    /// <summary>A new photo of the player's in the room, its id the name its files are kept under.</summary>
    Task<Guid> RecordPhotoAsync(PlayerData player, int roomId);

    /// <summary>The player's latest photo, if they took one.</summary>
    Task<Guid?> LatestPhotoAsync(PlayerData player);

    /// <summary>Buys the latest photo as a photo furni (its picture at <paramref name="photoUrl"/>), for credits and
    /// duckets.</summary>
    Task<PhotoPurchase> PurchaseAsync(PlayerData player, int creditPrice, int ducketPrice, Func<Guid, string> photoUrl);

    /// <summary>Whether the player may set the room's navigator thumbnail (its owner, or staff who pick rooms).</summary>
    Task<bool> MayPhotographRoomAsync(PlayerData player, int roomId);

    /// <summary>Publishes the latest photo, for duckets, at most once every <paramref name="cooldown"/>.</summary>
    Task<PhotoPublication> PublishAsync(PlayerData player, int ducketPrice, TimeSpan cooldown);
}
