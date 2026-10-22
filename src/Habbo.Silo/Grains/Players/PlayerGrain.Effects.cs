using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Inventory.AvatarEffect;
using Habbo.Silo.Abstractions.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Players;

/// <summary>The effects inventory: effects wait to be activated, then run down while the player
/// may wear them, and run out on their own.</summary>
public sealed partial class PlayerGrain
{
    private readonly Dictionary<int, IGrainTimer> _effectTimers = [];

    public async Task SendEffectsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await ExpireEffectsAsync(db);
        var now = DateTimeOffset.UtcNow;
        var effects = await db.UserEffects.AsNoTracking().Where(e => e.UserId == PlayerId).OrderBy(e => e.EffectId).ToListAsync();

        Send(new AvatarEffectsMessageComposer(effects.Select(effect => new AvatarEffectData(
                effect.EffectId,
                effect.SubType,
                effect.Duration,
                effect.Inactive,
                effect.ActivatedAt is null
                    ? AvatarEffectData.NotActive
                    : effect.ExpiresAt is { } expires ? (int)Math.Max(0, (expires - now).TotalSeconds) : effect.Duration,
                effect.IsPermanent))
            .ToList()));

        foreach (var effect in effects.Where(e => e.ExpiresAt is not null))
            ScheduleExpiry(effect.EffectId, effect.ExpiresAt!.Value);
    }

    public async Task AddEffectAsync(int effectId, int duration, bool isPermanent, int amount = 1)
    {
        if (amount < 1)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var effect = await db.UserEffects.FirstOrDefaultAsync(e => e.UserId == PlayerId && e.EffectId == effectId);
        if (effect is null)
        {
            effect = new UserEffect { UserId = PlayerId, EffectId = effectId };
            db.UserEffects.Add(effect);
        }

        effect.Duration = duration;
        effect.IsPermanent |= isPermanent;
        effect.Inactive += amount;
        await db.SaveChangesAsync();

        for (var i = 0; i < amount; i++)
            Send(new AvatarEffectAddedMessageComposer(effectId, effect.SubType, duration, isPermanent));
    }

    public async Task<bool> ActivateEffectAsync(int effectId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await ExpireEffectsAsync(db);
        var effect = await db.UserEffects.FirstOrDefaultAsync(e => e.UserId == PlayerId && e.EffectId == effectId);
        if (effect is null || effect.ActivatedAt is not null || effect.Inactive < 1)
            return false;

        effect.Inactive--;
        effect.ActivatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        Send(new AvatarEffectActivatedMessageComposer(effectId, effect.Duration, effect.IsPermanent));
        if (effect.ExpiresAt is { } expires)
            ScheduleExpiry(effectId, expires);
        return true;
    }

    public async Task<bool> CanWearEffectAsync(int effectId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await ExpireEffectsAsync(db);
        return await db.UserEffects.AnyAsync(e => e.UserId == PlayerId && e.EffectId == effectId && e.ActivatedAt != null);
    }

    public Task EffectSelectedAsync(int effectId)
    {
        Send(new AvatarEffectSelectedMessageComposer(effectId));
        return Task.CompletedTask;
    }

    private void ScheduleExpiry(int effectId, DateTimeOffset expiresAt)
    {
        if (_effectTimers.Remove(effectId, out var existing))
            existing.Dispose();

        var due = expiresAt - DateTimeOffset.UtcNow;
        _effectTimers[effectId] = this.RegisterGrainTimer(async _ =>
        {
            if (_effectTimers.Remove(effectId, out var timer))
                timer.Dispose();
            await using var db = await dbFactory.CreateDbContextAsync();
            await ExpireEffectsAsync(db);
        }, new GrainTimerCreationOptions { DueTime = due > TimeSpan.Zero ? due : TimeSpan.Zero, Period = Timeout.InfiniteTimeSpan });
    }

    /// <summary>Ends every running effect whose time is up: the inventory loses it, and the avatar
    /// takes it off.</summary>
    private async Task ExpireEffectsAsync(AppDbContext db)
    {
        var now = DateTimeOffset.UtcNow;
        var running = await db.UserEffects
            .Where(e => e.UserId == PlayerId && e.ActivatedAt != null && !e.IsPermanent)
            .ToListAsync();
        var expired = running.Where(e => e.ExpiresAt <= now).ToList();
        if (expired.Count == 0)
            return;

        foreach (var effect in expired)
        {
            effect.ActivatedAt = null;
            if (effect.Inactive == 0)
                db.UserEffects.Remove(effect);
        }

        await db.SaveChangesAsync();
        foreach (var effect in expired)
        {
            Send(new AvatarEffectExpiredMessageComposer(effect.EffectId));
            if (_roomId is { } roomId)
                await GrainFactory.GetGrain<IGameRoomGrain>(roomId).EffectExpiredAsync(PlayerId, effect.EffectId);
        }
    }
}
