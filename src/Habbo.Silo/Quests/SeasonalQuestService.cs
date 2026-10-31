using Habbo.Domain.Quests;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Quest;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Quests;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;
using Orleans;

namespace Habbo.Silo.Quests;

/// <summary>Seasonal quests (seed/seasonal_quests.json) and each player's progress on them: listing the campaigns
/// running, taking a quest on and putting it down, and counting towards it until it is done and pays.</summary>
public sealed class SeasonalQuestService(
    IDbContextFactory<AppDbContext> dbFactory, IGrainFactory grains, PlayerMessenger messenger, SeasonalQuestCatalog catalog)
    : ISeasonalQuestService
{
    /// <summary>Why the activity points moved (CurrencyTransaction.Reason).</summary>
    private const string RewardReason = "seasonal_quest_reward";

    /// <summary>Whether the client shows its quest completed dialog, with the reward.</summary>
    private const bool ShowCompletedDialog = true;

    private const bool Expired = true;

    public async Task SendSeasonalQuestsAsync(PlayerData player)
    {
        var now = DateTimeOffset.UtcNow;
        var campaigns = await catalog.AllAsync();
        await using var db = await dbFactory.CreateDbContextAsync();
        var progress = await ProgressOfAsync(db, player.Id);
        await PutDownExpiredAsync(db, player.Id, campaigns, progress, now);
        await messenger.SendAsync(player.Id, new SeasonalQuestsMessageComposer(SeasonalQuestRules.List(campaigns, progress, now)));
    }

    public async Task AcceptAsync(PlayerData player, int questId)
    {
        var now = DateTimeOffset.UtcNow;
        var campaigns = await catalog.AllAsync();
        await using var db = await dbFactory.CreateDbContextAsync();
        var progress = await ProgressOfAsync(db, player.Id);
        if (SeasonalQuestRules.Takeable(campaigns, questId, progress, now) is not ({ } campaign, { } quest))
            return;

        foreach (var other in progress.Values.Where(p => p.Accepted && p.QuestId != questId))
            other.Accepted = false;
        if (!progress.TryGetValue(questId, out var own))
        {
            own = new UserSeasonalQuest { UserId = player.Id, QuestId = questId };
            db.UserSeasonalQuests.Add(own);
            progress[questId] = own;
        }

        own.Accepted = true;
        await db.SaveChangesAsync();
        await messenger.SendAsync(player.Id, new QuestMessageComposer(SeasonalQuestRules.DataOf(campaign, progress, now)));

        // a badge already worn is a quest already done
        if (quest.Type == SeasonalQuestTypes.WearBadge
            && (await grains.GetGrain<IPlayerGrain>(player.Id).GetWornBadgesAsync(player.AccountId)).Any(b => b.Code == quest.Parameter))
            await ProgressAsync(player.Id, quest.Type, quest.Parameter, quest.Steps);
    }

    public async Task RejectAsync(PlayerData player, int? questId)
    {
        var now = DateTimeOffset.UtcNow;
        var campaigns = await catalog.AllAsync();
        await using var db = await dbFactory.CreateDbContextAsync();
        var progress = await ProgressOfAsync(db, player.Id);
        var taken = progress.Values.FirstOrDefault(p => p.Accepted && (questId is null || p.QuestId == questId));
        if (taken is null || SeasonalQuestRules.Find(campaigns, taken.QuestId) is not ({ } campaign, _))
            return;

        taken.Accepted = false;
        await db.SaveChangesAsync();
        await messenger.SendAsync(player.Id,
            new QuestCancelledMessageComposer(!Expired, SeasonalQuestRules.DataOf(campaign, progress, now)));
    }

    public async Task ProgressAsync(Guid playerId, string questType, string parameter, int amount)
    {
        var now = DateTimeOffset.UtcNow;
        var campaigns = await catalog.AllAsync();
        await using var db = await dbFactory.CreateDbContextAsync();
        var progress = await ProgressOfAsync(db, playerId);
        if (progress.Values.FirstOrDefault(p => p.Accepted) is not { } taken
            || SeasonalQuestRules.Find(campaigns, taken.QuestId) is not ({ } campaign, { } quest)
            || !quest.Counts(questType, parameter))
            return;

        if (!campaign.IsRunning(now))
        {
            await PutDownExpiredAsync(db, playerId, campaigns, progress, now);
            return;
        }

        var before = taken.Progress;
        var done = SeasonalQuestRules.Count(quest, taken, amount, now);
        if (!done && taken.Progress == before)
            return;

        await db.SaveChangesAsync();
        if (!done)
        {
            await messenger.SendAsync(playerId, new QuestMessageComposer(SeasonalQuestRules.DataOf(campaign, progress, now)));
            return;
        }

        if (SeasonalQuestRules.Pays(quest))
            await grains.GetGrain<IPlayerGrain>(playerId).AddActivityPointsAsync(quest.ActivityPointType, quest.Reward, RewardReason);
        await messenger.SendAsync(playerId, new QuestCompletedMessageComposer(
            SeasonalQuestRules.DataOf(campaign, quest, SeasonalQuestRules.DoneCount(campaign, progress), quest.Id, accepted: false,
                quest.Steps, now),
            ShowCompletedDialog));
    }

    /// <summary>A quest taken on whose campaign has ended is put down, and the player told it expired.</summary>
    private async Task PutDownExpiredAsync(AppDbContext db, Guid playerId, IReadOnlyList<SeasonalCampaign> campaigns,
        IReadOnlyDictionary<int, UserSeasonalQuest> progress, DateTimeOffset now)
    {
        foreach (var taken in progress.Values.Where(p => p.Accepted))
        {
            if (SeasonalQuestRules.Find(campaigns, taken.QuestId) is ({ } campaign, _) && campaign.IsRunning(now))
                continue;

            taken.Accepted = false;
            await db.SaveChangesAsync();
            if (SeasonalQuestRules.Find(campaigns, taken.QuestId) is ({ } ended, _))
                await messenger.SendAsync(playerId, new QuestCancelledMessageComposer(Expired, SeasonalQuestRules.DataOf(ended, progress, now)));
        }
    }

    private static Task<Dictionary<int, UserSeasonalQuest>> ProgressOfAsync(AppDbContext db, Guid playerId) =>
        db.UserSeasonalQuests.Where(q => q.UserId == playerId).ToDictionaryAsync(q => q.QuestId);
}
