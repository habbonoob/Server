using Habbo.Domain.Quests;
using Habbo.Protocol.Messages.Outgoing.Quest;

namespace Habbo.Silo.Quests;

/// <summary>Where a player is in each seasonal campaign, from their progress on its quests (by quest id): on its first
/// quest not yet done, or through it. Only a running campaign's quest may be taken on, one quest at a time.</summary>
public static class SeasonalQuestRules
{
    /// <summary>The quest id a campaign that is done is sent with: the client draws any id below 1 as done.</summary>
    public const int CampaignDone = 0;

    public const bool Seasonal = true;

    public static bool IsDone(UserSeasonalQuest? progress) => progress?.CompletedAt is not null;

    /// <summary>The quest the player is on in the campaign: its first not done, or null once all are.</summary>
    public static SeasonalQuest? CurrentQuest(SeasonalCampaign campaign, IReadOnlyDictionary<int, UserSeasonalQuest> progress) =>
        campaign.Quests.FirstOrDefault(q => !IsDone(progress.GetValueOrDefault(q.Id)));

    public static int DoneCount(SeasonalCampaign campaign, IReadOnlyDictionary<int, UserSeasonalQuest> progress) =>
        campaign.Quests.Count(q => IsDone(progress.GetValueOrDefault(q.Id)));

    /// <summary>The running campaigns, each with its quest the player is on, in the order the calendar sorts them.</summary>
    public static IReadOnlyList<QuestMessageData> List(IEnumerable<SeasonalCampaign> campaigns,
        IReadOnlyDictionary<int, UserSeasonalQuest> progress, DateTimeOffset now) =>
        campaigns.Where(c => c.IsRunning(now) && c.Quests.Count > 0)
            .Select(c => DataOf(c, progress, now))
            .ToList();

    /// <summary>The campaign as the player stands in it: its current quest, or its last with no id once it is done.</summary>
    public static QuestMessageData DataOf(SeasonalCampaign campaign, IReadOnlyDictionary<int, UserSeasonalQuest> progress, DateTimeOffset now)
    {
        var current = CurrentQuest(campaign, progress);
        var quest = current ?? campaign.Quests[^1];
        var own = progress.GetValueOrDefault(quest.Id);
        return DataOf(campaign, quest, DoneCount(campaign, progress), current is null ? CampaignDone : quest.Id,
            accepted: current is not null && own?.Accepted == true,
            completedSteps: current is null ? quest.Steps : Math.Min(quest.Steps, own?.Progress ?? 0), now);
    }

    /// <summary>One quest of the campaign, as far as the player has got with it.</summary>
    public static QuestMessageData DataOf(SeasonalCampaign campaign, SeasonalQuest quest, int doneCount, int id, bool accepted,
        int completedSteps, DateTimeOffset now) =>
        new(campaign.Code, doneCount, campaign.Quests.Count, quest.ActivityPointType, id, accepted, quest.Type, quest.ImageVersion,
            quest.Reward, quest.LocalizationCode, completedSteps, quest.Steps, quest.SortOrder, quest.CatalogPage, campaign.ChainCode,
            quest.Easy, Seasonal, SecondsLeft(campaign, now));

    public static int SecondsLeft(SeasonalCampaign campaign, DateTimeOffset now) =>
        (int)Math.Clamp((campaign.EndsAt - now).TotalSeconds, 0, int.MaxValue);

    /// <summary>The quest with that id in a running campaign, if it is the one the player is on there.</summary>
    public static (SeasonalCampaign Campaign, SeasonalQuest Quest)? Takeable(IEnumerable<SeasonalCampaign> campaigns, int questId,
        IReadOnlyDictionary<int, UserSeasonalQuest> progress, DateTimeOffset now)
    {
        foreach (var campaign in campaigns.Where(c => c.IsRunning(now)))
        {
            if (CurrentQuest(campaign, progress) is { } quest && quest.Id == questId)
                return (campaign, quest);
        }

        return null;
    }

    /// <summary>The campaign and quest a quest id belongs to, running or not.</summary>
    public static (SeasonalCampaign Campaign, SeasonalQuest Quest)? Find(IEnumerable<SeasonalCampaign> campaigns, int questId)
    {
        foreach (var campaign in campaigns)
        {
            if (campaign.Quests.FirstOrDefault(q => q.Id == questId) is { } quest)
                return (campaign, quest);
        }

        return null;
    }

    /// <summary>Counts what the player did on the quest they are on, up to its steps; true once that makes it done.</summary>
    public static bool Count(SeasonalQuest quest, UserSeasonalQuest progress, int amount, DateTimeOffset now)
    {
        progress.Progress = Math.Clamp(progress.Progress + Math.Max(0, amount), 0, quest.Steps);
        if (progress.Progress < quest.Steps)
            return false;

        progress.CompletedAt = now;
        progress.Accepted = false;
        return true;
    }

    /// <summary>Whether the quest pays activity points when done.</summary>
    public static bool Pays(SeasonalQuest quest) => quest.ActivityPointType >= 0 && quest.Reward > 0;
}
