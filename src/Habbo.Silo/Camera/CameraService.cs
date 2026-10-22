using Habbo.Silo.Abstractions.Permissions;
using System.Text.Json;
using Habbo.Domain.Camera;
using Habbo.Domain.Furniture;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Camera;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Achievements;
using Habbo.Silo.Permissions;
using Microsoft.EntityFrameworkCore;
using Orleans;

namespace Habbo.Silo.Camera;

/// <summary>The camera's photos, in the photos table: recorded as they are rendered, bought as photo furni (a wall
/// poster showing the photo), published to the player's stories.</summary>
public sealed class CameraService(IDbContextFactory<AppDbContext> dbFactory, IGrainFactory grains, PermissionService permissions) : ICameraService
{
    /// <summary>The furni a bought photo is: a picture on the wall, its image the photo's (its extra data says where).</summary>
    public const string PhotoClassName = "external_image_wallitem_poster_small";

    private const int DucketType = (int)Habbo.Domain.Catalog.ActivityPointType.Ducket;

    public async Task<Guid> RecordPhotoAsync(PlayerData player, int roomId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var photo = new Photo { Id = Guid.NewGuid(), UserId = player.Id, RoomId = roomId };
        db.Photos.Add(photo);
        await db.SaveChangesAsync();
        return photo.Id;
    }

    public async Task<Guid?> LatestPhotoAsync(PlayerData player)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return (await LatestAsync(db, player))?.Id;
    }

    public async Task<PhotoPurchase> PurchaseAsync(PlayerData player, int creditPrice, int ducketPrice, Func<Guid, string> photoUrl)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await LatestAsync(db, player) is not { } photo)
            return PhotoPurchase.Nothing;
        var definition = await db.ItemDefinitions.AsNoTracking()
                             .FirstOrDefaultAsync(d => d.ClassName == PhotoClassName && d.Type == ItemType.Wall)
                         ?? throw new InvalidOperationException($"There is no {PhotoClassName} wall item to make photos of.");

        var grain = grains.GetGrain<IPlayerGrain>(player.Id);
        if (creditPrice > 0 && await grain.SpendCreditsAsync(creditPrice, CurrencyTransactionReasons.PhotoPurchase) is null)
            return PhotoPurchase.ShortOfCredits;
        if (ducketPrice > 0 && !await grain.SpendActivityPointsAsync(DucketType, ducketPrice, CurrencyTransactionReasons.PhotoPurchase))
        {
            // the credits were taken first; they go back when the duckets fall short
            if (creditPrice > 0)
                await grain.AddCreditsAsync(creditPrice, CurrencyTransactionReasons.PhotoPurchase);
            return PhotoPurchase.ShortOfDuckets;
        }

        photo.Purchases++;
        await db.SaveChangesAsync();
        var items = await grain.ReceiveFurnitureAsync(definition.Id, ExtraData(photo, player, photoUrl(photo.Id)), 1);
        await grain.ProgressAchievementAsync(AchievementNames.CameraPhotoCount, 1);
        return PhotoPurchase.Bought(items);
    }

    public async Task<PhotoPublication> PublishAsync(PlayerData player, int ducketPrice, TimeSpan cooldown)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await LatestAsync(db, player) is not { } photo)
            return new PhotoPublication(false, 0, null);
        if (photo.PublishedAt is not null)
            return new PhotoPublication(true, 0, photo.Id);

        var last = await db.Photos.Where(p => p.UserId == player.Id && p.PublishedAt != null)
            .MaxAsync(p => (DateTimeOffset?)p.PublishedAt);
        var wait = last is null ? TimeSpan.Zero : last.Value + cooldown - DateTimeOffset.UtcNow;
        if (wait > TimeSpan.Zero)
            return new PhotoPublication(false, (int)Math.Ceiling(wait.TotalSeconds), null);

        if (ducketPrice > 0 && !await grains.GetGrain<IPlayerGrain>(player.Id)
                .SpendActivityPointsAsync(DucketType, ducketPrice, CurrencyTransactionReasons.PhotoPublish))
            return new PhotoPublication(false, 0, null);

        photo.PublishedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return new PhotoPublication(true, 0, photo.Id);
    }

    public async Task<bool> MayPhotographRoomAsync(PlayerData player, int roomId)
    {
        if (permissions.Has(player, PermissionNames.NavigatorStaffPick))
            return true;
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Rooms.AnyAsync(r => r.Id == roomId && r.OwnerId == player.Id);
    }

    private static Task<Photo?> LatestAsync(AppDbContext db, PlayerData player) =>
        db.Photos.Where(p => p.UserId == player.Id).OrderByDescending(p => p.CreatedAt).FirstOrDefaultAsync();

    /// <summary>What a photo furni carries, as the hotel's do: "w" its picture's url (the client shows its "_small"
    /// form on the wall), "u" the photo's id, "t" when it was taken (seconds), "n" whose it is, "s" the room.</summary>
    private static string ExtraData(Photo photo, PlayerData owner, string url) => JsonSerializer.Serialize(new Dictionary<string, object>
    {
        ["w"] = url,
        ["u"] = photo.Id.ToString(),
        ["t"] = photo.CreatedAt.ToUnixTimeSeconds(),
        ["n"] = owner.Username,
        ["s"] = photo.RoomId,
    });
}
