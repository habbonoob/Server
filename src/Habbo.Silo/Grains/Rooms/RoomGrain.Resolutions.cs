using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Achievements;
using Habbo.Silo.Items;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>Resolution statues in the room: their owner picks a challenge on one and follows it there, and a statue
/// whose time runs out fails where it stands. Meeting a challenge is the player's side
/// (<see cref="Players.PlayerGrain"/>), as a level can be reached anywhere.</summary>
public sealed partial class RoomGrain
{
    /// <summary>How often the room looks for challenges that ran out.</summary>
    private static readonly TimeSpan ResolutionCheckInterval = TimeSpan.FromMinutes(1);

    private DateTimeOffset _nextResolutionCheck = DateTimeOffset.MinValue;

    public async Task OpenResolutionAsync(Guid playerId, int itemId, int achievementId)
    {
        if (OwnStatue(playerId, itemId) is not { } statue)
            return;

        var state = BadgeEngraving.StateOf(statue.Data, (int)AchievementResolutionState.NotStarted);
        if (state is not ((int)AchievementResolutionState.NotStarted or (int)AchievementResolutionState.InProgress))
            return;

        // the challenge is saved under the statue's database id; the client knows it by the room's
        var storedId = statue.Item.Id;
        if (await resolutions.FindAsync(storedId) is { } resolution)
        {
            // a challenge follows the one who took it on: a statue that changed hands starts over
            if (resolution.UserId != playerId)
            {
                await resolutions.EndAsync(storedId);
                await SetResolutionStateAsync(statue, AchievementResolutionState.NotStarted);
            }
            else if (resolution.EndsAt <= DateTimeOffset.UtcNow)
            {
                await FailResolutionAsync(statue);
                return;
            }
            else
            {
                if (await resolutions.ProgressAsync(resolution, statue.Id) is { } progress)
                    SendTo(playerId, progress);
                return;
            }
        }
        else if (state != (int)AchievementResolutionState.NotStarted)
        {
            await SetResolutionStateAsync(statue, AchievementResolutionState.NotStarted);
        }

        if (achievementId == 0)
        {
            SendTo(playerId, await resolutions.OffersAsync(playerId, statue.Id));
            return;
        }

        if (await resolutions.StartAsync(playerId, storedId, achievementId) is not { } started)
            return;

        await SetResolutionStateAsync(statue, AchievementResolutionState.InProgress);
        if (await resolutions.ProgressAsync(started, statue.Id) is { } first)
            SendTo(playerId, first);
    }

    public async Task ResetResolutionAsync(Guid playerId, int itemId)
    {
        if (OwnStatue(playerId, itemId) is not { } statue || await resolutions.FindAsync(statue.Item.Id) is not { } resolution
                                                           || resolution.UserId != playerId)
            return;

        await resolutions.EndAsync(statue.Item.Id);
        await SetResolutionStateAsync(statue, AchievementResolutionState.NotStarted);
    }

    /// <summary>Another grain changes an item of this room's data, naming it by its database id.</summary>
    public Task<bool> ReplaceItemDataAsync(int itemId, FurniDataView data)
    {
        if (_items?.FindStored(itemId) is not { } item || _furniture is null)
            return Task.FromResult(false);

        // this call interleaves, so it may land in the middle of the room's tick; the change itself (which can set off
        // wired that moves furni and users) waits for the room's own turn, on its next tick
        var furniture = _furniture;
        Schedule(TimeSpan.Zero, async () =>
        {
            if (_items?.FindStored(itemId) == item)
                await furniture.UpdateAsync(item, data);
        });
        return Task.FromResult(true);
    }

    public Task<int> StoredIdOfAsync(int itemId) => Task.FromResult(_items?.StoredIdOf(itemId) ?? itemId);

    /// <summary>Fails the statues here whose challenge ran out; looked for now and then, on the tick.</summary>
    private async Task ExpireResolutionsAsync()
    {
        var now = DateTimeOffset.UtcNow;
        if (now < _nextResolutionCheck || _items is null)
            return;

        _nextResolutionCheck = now + ResolutionCheckInterval;
        var underWay = _items.Items
            .Where(i => i.Interaction is AchievementResolutionInteraction
                        && BadgeEngraving.StateOf(i.Data, (int)AchievementResolutionState.NotStarted) == (int)AchievementResolutionState.InProgress)
            .Select(i => i.Item.Id)
            .ToList();
        if (underWay.Count == 0)
            return;

        foreach (var resolution in await resolutions.UnderWayAsync(underWay))
        {
            if (resolution.EndsAt <= now && _items.FindStored(resolution.ItemId) is { } statue)
                await FailResolutionAsync(statue);
        }
    }

    private RoomItemStore.PlacedItem? OwnStatue(Guid playerId, int itemId) =>
        _entities?.Find(playerId) is not null && _items?.Find(itemId) is { Interaction: AchievementResolutionInteraction } statue
                                             && statue.Item.OwnerId == playerId
            ? statue
            : null;

    private async Task FailResolutionAsync(RoomItemStore.PlacedItem statue)
    {
        await resolutions.EndAsync(statue.Item.Id);
        await SetResolutionStateAsync(statue, AchievementResolutionState.Failed);
    }

    private Task SetResolutionStateAsync(RoomItemStore.PlacedItem statue, AchievementResolutionState state) =>
        _furniture!.UpdateAsync(statue, BadgeEngraving.Blank((int)state));
}
