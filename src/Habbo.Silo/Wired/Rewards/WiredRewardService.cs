using Habbo.Domain.Furniture.Data;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;
using Habbo.Silo.Abstractions.Hotel;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Rooms;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Habbo.Silo.Wired.Rewards;

/// <summary>Gives a give reward box's rewards. The box keeps a record of every reward it gave, which its limits are
/// counted from: how many it gives in all, how often one user is rewarded, and, for unique rewards, which each user has
/// had. A reward is drawn by its chance, or, when unique, from those the user has not had; a badge they have already
/// is none they can be given.</summary>
public sealed class WiredRewardService(
    IDbContextFactory<AppDbContext> dbFactory,
    IGrainFactory grains,
    PlayerMessenger messenger,
    IGameTexts texts,
    ILogger<WiredRewardService> logger) : IWiredRewards
{
    /// <summary>The chances are out of a hundred.</summary>
    private const int ChanceOutOf = 100;

    /// <summary>The game text every badge has, its name, by which a badge code is known to be one.</summary>
    private const string BadgeNameKey = "badge_name_";

    public async Task RewardAsync(int boxId, RoomEntityStore.RoomEntity user, WiredRewardRules rules, Random random)
    {
        if (user.Bot is not null || user.Pet is not null || rules.Rewards.Count == 0)
            return;

        var userId = user.Player.Id;
        var player = grains.GetGrain<IPlayerGrain>(userId);
        await using var db = await dbFactory.CreateDbContextAsync();
        var given = db.WiredRewardGrants.Where(g => g.WiredItemId == boxId);
        if (rules.Limit > 0 && await given.CountAsync() >= rules.Limit)
        {
            await TellAsync(userId, WiredRewardResultMessageComposer.LimitReached);
            return;
        }

        var mine = await given.AsNoTracking().Where(g => g.UserId == userId).OrderByDescending(g => g.GivenAt).ToListAsync();
        if (TooSoon(rules, mine.FirstOrDefault()) is { } wait)
        {
            await TellAsync(userId, wait);
            return;
        }

        WiredReward? reward;
        if (rules.Unique)
        {
            var left = new List<WiredReward>();
            foreach (var candidate in rules.Rewards)
            {
                if (!mine.Any(g => g.IsBadge == candidate.IsBadge && g.Code == candidate.Code)
                    && !(candidate.IsBadge && await player.HasBadgeAsync(candidate.Code)))
                    left.Add(candidate);
            }

            if (left.Count == 0)
            {
                await TellAsync(userId, WiredRewardResultMessageComposer.AllCollected);
                return;
            }

            reward = left[random.Next(left.Count)];
        }
        else
        {
            reward = Draw(rules.Rewards, random.Next(ChanceOutOf));
            if (reward is null)
            {
                await TellAsync(userId, WiredRewardResultMessageComposer.Unlucky);
                return;
            }
        }

        if (reward.IsBadge)
        {
            if (!await player.GiveBadgeAsync(reward.Code))
            {
                await TellAsync(userId, WiredRewardResultMessageComposer.AlreadyGiven);
                return;
            }
        }
        else
        {
            if (await DefinitionOfAsync(db, reward.Code) is not { } definitionId)
            {
                logger.LogWarning("Give reward box {BoxId} names a product {Code} the hotel no longer has", boxId, reward.Code);
                return;
            }

            await player.ReceiveFurnitureAsync(definitionId, string.Empty, 1);
        }

        db.WiredRewardGrants.Add(new WiredRewardGrant { WiredItemId = boxId, UserId = userId, IsBadge = reward.IsBadge, Code = reward.Code });
        await db.SaveChangesAsync();
        await TellAsync(userId, reward.IsBadge ? WiredRewardResultMessageComposer.BadgeReceived : WiredRewardResultMessageComposer.ProductReceived);
    }

    public async Task<IReadOnlyList<string>> MissingAsync(IReadOnlyList<WiredReward> rewards)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var missing = new List<string>();
        foreach (var reward in rewards)
        {
            var exists = reward.IsBadge
                ? await texts.GetAsync(BadgeNameKey + reward.Code) is not null
                : await DefinitionOfAsync(db, reward.Code) is not null;
            if (!exists)
                missing.Add(reward.Code);
        }

        return missing;
    }

    private static Task<int?> DefinitionOfAsync(AppDbContext db, string code) =>
        db.ItemDefinitions.AsNoTracking().Where(d => d.ClassName == code).Select(d => (int?)d.Id).FirstOrDefaultAsync();

    /// <summary>Why the user may not be rewarded again yet, if they may not.</summary>
    private static int? TooSoon(WiredRewardRules rules, WiredRewardGrant? last)
    {
        if (last is null)
            return null;
        if (rules.Wait is not { } wait)
            return WiredRewardResultMessageComposer.AlreadyGiven;
        if (DateTimeOffset.UtcNow - last.GivenAt >= wait)
            return null;

        return rules.Interval switch
        {
            WiredRewardRules.Days => WiredRewardResultMessageComposer.GivenToday,
            WiredRewardRules.Hours => WiredRewardResultMessageComposer.GivenThisHour,
            _ => WiredRewardResultMessageComposer.GivenThisMinute,
        };
    }

    /// <summary>The reward a roll lands on, each taking its chance's share of the hundred in turn; none past them all.</summary>
    private static WiredReward? Draw(IReadOnlyList<WiredReward> rewards, int roll)
    {
        var upTo = 0;
        foreach (var reward in rewards)
        {
            upTo += reward.Probability;
            if (roll < upTo)
                return reward;
        }

        return null;
    }

    private Task TellAsync(Guid userId, int reason) => messenger.SendAsync(userId, new WiredRewardResultMessageComposer(reason));
}
