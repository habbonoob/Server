using Habbo.Domain.Rooms;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Navigator;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Grains.Rooms;
using Habbo.Silo.Navigator;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;

namespace Habbo.Silo.Grains.Navigator;

/// <summary>The navigator's stateful side: starred rooms, the tag cloud, and room creation.
/// Stateless worker — every call reads and writes the database directly.</summary>
[StatelessWorker]
public sealed class NavigatorGrain(
    IDbContextFactory<AppDbContext> dbFactory,
    IOptions<NavigatorOptions> options,
    RoomSettingsRules settingsRules,
    ILogger<NavigatorGrain> logger) : Grain, INavigatorGrain
{
    public async Task<List<int>> GetFavouritesAsync(Guid userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.RoomFavourites
            .AsNoTracking()
            .Where(favourite => favourite.UserId == userId)
            .OrderBy(favourite => favourite.AddedAt)
            .Select(favourite => favourite.RoomId)
            .ToListAsync();
    }

    public Task<int> GetFavouriteLimitAsync() => Task.FromResult(options.Value.FavouriteLimit);

    public async Task<bool> SetFavouriteAsync(Guid userId, int roomId, bool isFavourite)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var existing = await db.RoomFavourites
            .FirstOrDefaultAsync(favourite => favourite.UserId == userId && favourite.RoomId == roomId);

        if (!isFavourite)
        {
            if (existing is null)
                return false;

            db.RoomFavourites.Remove(existing);
            await db.SaveChangesAsync();
            return true;
        }

        if (existing is not null)
            return true;

        if (!await db.Rooms.AnyAsync(room => room.Id == roomId))
            return false;

        var count = await db.RoomFavourites.CountAsync(favourite => favourite.UserId == userId);
        if (count >= options.Value.FavouriteLimit)
            return false;

        db.RoomFavourites.Add(new RoomFavourite { UserId = userId, RoomId = roomId });
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<RoomTagCount>> GetPopularTagsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var tagLists = await db.Rooms
            .AsNoTracking()
            .Where(room => room.Tags.Count > 0)
            .Select(room => room.Tags)
            .ToListAsync();

        return tagLists
            .SelectMany(tags => tags)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .GroupBy(tag => tag.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(options.Value.PopularTagCount)
            .Select(group => new RoomTagCount(group.Key, group.Count()))
            .ToList();
    }

    public async Task<RoomCreationResult?> CreateRoomAsync(PlayerData owner, RoomCreationRequest request)
    {
        // a new room's settings are held to what changing them later would be
        var name = settingsRules.Name(request.Name);
        var description = settingsRules.Description(request.Description);
        if (name.Length == 0 || !await settingsRules.IsCleanAsync(name) || !await settingsRules.IsCleanAsync(description)
            || !await settingsRules.MayPickCategoryAsync(owner, request.CategoryId))
        {
            logger.LogInformation("Room creation by {Owner} refused: name, description or category not allowed.", owner.Id);
            return null;
        }

        await using var db = await dbFactory.CreateDbContextAsync();

        // another room's own plan is not one to build on: its floor plan editor changes it in place
        var model = request.ModelName.StartsWith(RoomGrain.CustomModelPrefix, StringComparison.OrdinalIgnoreCase)
            ? null
            : await db.RoomModels.FirstOrDefaultAsync(m => m.Name == request.ModelName);
        if (model is null)
        {
            logger.LogWarning("Room creation asked for unknown model '{Model}'.", request.ModelName);
            return null;
        }

        var room = new Room
        {
            OwnerId = owner.Id,
            ModelId = model.Id,
            Name = name,
            Description = description,
            CategoryId = request.CategoryId,
            MaxUsers = settingsRules.MaxVisitors(request.MaxVisitors),
            TradeMode = RoomSettingsRules.Choice(request.TradeMode, 0, RoomSettingsRules.TradeModeMax, 0),
        };

        db.Rooms.Add(room);
        await db.SaveChangesAsync();
        logger.LogInformation("Created room {RoomId} '{Name}' on model '{Model}'.",
            room.Id, room.Name, model.Name);

        return new RoomCreationResult(room.Id, room.Name);
    }
}
