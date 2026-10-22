using Habbo.Domain.Users;
using Habbo.Protocol.Messages.Outgoing.Room.Action;
using Habbo.Protocol.Messages.Outgoing.Room.Chat;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Messages.Outgoing.Users;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>What the room shows of its users' accounts: their looks, names, badges and effects,
/// what they carry, and whether they are typing.</summary>
public sealed partial class RoomGrain
{
    public Task RefreshPlayerAsync(Guid playerId, PlayerData player)
    {
        if (_entities?.Find(playerId) is { } entity)
        {
            // a new look of one's own takes off whatever the room had them wearing
            entity.Player = player;
            entity.RoomOutfit = null;
            Broadcast(new UserChangeMessageComposer(
                entity.RoomIndex, player.Figure, player.Gender.ToLetter(), player.Motto, player.AchievementScore,
                BadgesRank: entity.BadgesRank));
        }

        return Task.CompletedTask;
    }

    public Task RenamePlayerAsync(Guid playerId, PlayerData player)
    {
        if (_entities?.Find(playerId) is { } entity)
        {
            entity.Player = player;
            Broadcast(new UserNameChangedMessageComposer(player.AccountId, entity.RoomIndex, player.Username));
        }

        return Task.CompletedTask;
    }

    public Task ShowWornBadgesAsync(Guid playerId, IReadOnlyList<WornBadge> badges)
    {
        if (_entities?.Find(playerId) is { } entity)
        {
            entity.WornBadges = badges.OrderBy(b => b.Slot).Select(b => b.Code).ToList();
            Broadcast(HabboUserBadgesMessageComposer.From(entity.Player.AccountId, badges));
        }

        return Task.CompletedTask;
    }

    public Task WearEffectAsync(Guid playerId, int effectId)
    {
        if (_entities?.Find(playerId) is { } entity)
            ApplyEffect(entity, effectId);
        return Task.CompletedTask;
    }

    public Task EffectExpiredAsync(Guid playerId, int effectId)
    {
        if (_entities?.Find(playerId) is { } entity && entity.EffectId == effectId)
            ApplyEffect(entity, NoEffect);
        return Task.CompletedTask;
    }

    /// <summary>The effect id that takes an effect off.</summary>
    private const int NoEffect = 0;

    /// <summary>The hand item that is an empty hand.</summary>
    private const int EmptyHand = 0;

    public Task DropHandItemAsync(Guid playerId)
    {
        if (_entities?.Find(playerId) is { } entity && !HandItemsBlockedFor(entity))
            SetHandItem(entity, EmptyHand);
        return Task.CompletedTask;
    }

    public async Task PassHandItemAsync(Guid playerId, int targetAccountId)
    {
        if (_entities?.Find(playerId) is not { } giver || giver.HandItem == EmptyHand || HandItemsBlockedFor(giver)
            || _entities.Entities.FirstOrDefault(e => e.Player.AccountId == targetAccountId) is not { } receiver
            || receiver == giver)
            return;

        // the giver walks up to the receiver, then hands it over if they are still next to each other
        var (x, y) = NearestFreeNeighbour(receiver, giver);
        await WalkThenAsync(giver, x, y, () =>
        {
            if (_entities.Find(giver.Player.Id) is null || _entities.Find(receiver.Player.Id) is null
                || giver.HandItem == EmptyHand
                || Math.Abs(giver.X - receiver.X) > 1 || Math.Abs(giver.Y - receiver.Y) > 1)
                return Task.CompletedTask;

            var handItem = giver.HandItem;
            LookAt(giver, receiver.X, receiver.Y);
            SetHandItem(giver, EmptyHand);
            SetHandItem(receiver, handItem);
            SendTo(receiver.Player.Id, new HandItemReceivedMessageComposer(giver.RoomIndex, handItem));
            return Task.CompletedTask;
        });
    }

    /// <summary>The walkable tile next to <paramref name="target"/> closest to <paramref name="walker"/>;
    /// the target's own tile when none is free, which walks as close as it can.</summary>
    private (int X, int Y) NearestFreeNeighbour(RoomEntityStore.RoomEntity target, RoomEntityStore.RoomEntity walker)
    {
        if (Math.Abs(walker.X - target.X) <= 1 && Math.Abs(walker.Y - target.Y) <= 1)
            return (walker.X, walker.Y);

        return Enumerable.Range(0, 8)
            .Select(RoomEntityStore.Offset)
            .Select(offset => (X: target.X + offset.X, Y: target.Y + offset.Y))
            .Where(tile => _model!.IsTile(tile.X, tile.Y) && !_items!.BlocksWalking(tile.X, tile.Y)
                           && !_entities!.HasAvatarAt(tile.X, tile.Y))
            .OrderBy(tile => Math.Abs(tile.X - walker.X) + Math.Abs(tile.Y - walker.Y))
            .Select(tile => ((int X, int Y)?)tile)
            .FirstOrDefault() ?? (target.X, target.Y);
    }

    public Task SetTypingAsync(Guid playerId, bool typing)
    {
        if (_entities?.Find(playerId) is { } entity)
        {
            Broadcast(new UserTypingMessageComposer(
                entity.RoomIndex, typing ? UserTypingMessageComposer.Typing : UserTypingMessageComposer.NotTyping));
        }

        return Task.CompletedTask;
    }

    /// <summary>Walks the avatar to a tile and then does something, unless the player walks off
    /// somewhere else first.</summary>
    private async Task WalkThenAsync(RoomEntityStore.RoomEntity entity, int x, int y, Func<Task> then)
    {
        entity.OnArrival = null;
        if (entity.X == x && entity.Y == y)
        {
            await then();
            return;
        }

        if (_entities!.Walk(entity, x, y))
            entity.OnArrival = then;
    }

    private void LookAt(RoomEntityStore.RoomEntity entity, int x, int y)
    {
        if (_entities?.LookTo(entity, x, y) is { } update)
            Broadcast(UserUpdateMessageComposer.From([update]));
    }

    private void SetHandItem(RoomEntityStore.RoomEntity entity, int handItem)
    {
        if (entity.HandItem == handItem)
            return;

        entity.HandItem = handItem;
        Broadcast(new CarryObjectMessageComposer(entity.RoomIndex, handItem));
    }
}
