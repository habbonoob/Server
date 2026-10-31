using System.Buffers;
using Habbo.Domain.Quests;
using Habbo.Infrastructure;
using Habbo.Protocol.IO;
using Habbo.Protocol.Messages.Outgoing.Quest;
using Habbo.Silo.Quests;
using Microsoft.Extensions.Configuration;

namespace Habbo.Silo.Tests;

public class SeasonalQuestTests
{
    /// <summary>When habbo.com answered GetSeasonalQuests on 25 Oct 2026.</summary>
    private static readonly DateTimeOffset Captured = DateTimeOffset.Parse("2026-10-25T08:27:25.174Z");

    /// <summary>What it answered: the two Super Habbo World campaigns, each at its first quest.</summary>
    private const string CapturedPayload =
        "000000020010626177636F6C6C61625F776F726C6473000000000000000AFFFFFFFF0000082E00000A574541525F4241444745000000000000000D3137" +
        "3731353739343437333835000000000000000100000001" + "0000000D31373731353739323336353931010100B42692000D626177636F6C6C61625F646C63" +
        "0000000000000003FFFFFFFF0000082F00000A574541525F4241444745000000000000000D3137373135383338373039393400000000000000010000" +
        "00010000000D31373731353833373237333630010100B42692";

    private static readonly Dictionary<int, UserSeasonalQuest> NoProgress = [];

    private static Task<IReadOnlyList<SeasonalCampaign>> SeedAsync() =>
        new SeasonalQuestCatalog(new SeedFiles(new ConfigurationBuilder().Build())).AllAsync();

    private static Dictionary<int, UserSeasonalQuest> Done(params int[] questIds) =>
        questIds.ToDictionary(id => id, id => new UserSeasonalQuest { QuestId = id, Progress = 1, CompletedAt = Captured });

    [Fact]
    public async Task ANewPlayerGetsWhatHabboSentOnTheDayOfTheCapture()
    {
        var quests = SeasonalQuestRules.List(await SeedAsync(), NoProgress, Captured);

        var buffer = new ArrayBufferWriter<byte>();
        new SeasonalQuestsMessageComposer(quests).Compose(new MessageWriter(buffer));
        Assert.Equal(CapturedPayload, Convert.ToHexString(buffer.WrittenSpan));
    }

    [Fact]
    public async Task TheAdventCalendarOpensADayADayFromItsFirst()
    {
        var calendar = (await SeedAsync()).Where(c => c.Code.StartsWith("xmas2013_", StringComparison.Ordinal)).ToList();
        Assert.Equal(24, calendar.Count);
        Assert.Equal(Enumerable.Range(1, 24), calendar.Select(c => c.Quests.Single().SortOrder));
        // day 21 is the campaign Habbo's texts call Day 21: xmas2013_22
        Assert.Equal("xmas2013_22", calendar[20].Code);

        var third = new DateTimeOffset(2026, 12, 3, 12, 0, 0, TimeSpan.Zero);
        var open = SeasonalQuestRules.List(calendar, NoProgress, third);
        Assert.Equal([1, 2, 3], open.Select(q => q.SortOrder));
        Assert.All(open, q => Assert.Equal((101, 5, true), (q.ActivityPointType, q.RewardCurrencyAmount, q.IsSeasonal)));

        Assert.Empty(SeasonalQuestRules.List(calendar, NoProgress, new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public async Task ACampaignMovesOnToItsNextQuestAndShowsDoneWithNoIdAtTheEnd()
    {
        var dlc = (await SeedAsync()).Single(c => c.Code == "bawcollab_dlc");
        var ids = dlc.Quests.Select(q => q.Id).ToArray();

        var second = SeasonalQuestRules.DataOf(dlc, Done(ids[0]), Captured);
        Assert.Equal((ids[1], 1, 2, "1771584000727"), (second.Id, second.CompletedQuestsInCampaign, second.SortOrder, second.LocalizationCode));

        var done = SeasonalQuestRules.DataOf(dlc, Done(ids), Captured);
        Assert.Equal((SeasonalQuestRules.CampaignDone, 3, 3), (done.Id, done.CompletedQuestsInCampaign, done.QuestCountInCampaign));
        Assert.Equal(done.TotalSteps, done.CompletedSteps);
    }

    [Fact]
    public async Task OnlyTheQuestTheCampaignIsOnCanBeTakenOn()
    {
        var campaigns = await SeedAsync();
        var worlds = campaigns.Single(c => c.Code == "bawcollab_worlds");

        Assert.NotNull(SeasonalQuestRules.Takeable(campaigns, worlds.Quests[0].Id, NoProgress, Captured));
        Assert.Null(SeasonalQuestRules.Takeable(campaigns, worlds.Quests[1].Id, NoProgress, Captured));
        Assert.NotNull(SeasonalQuestRules.Takeable(campaigns, worlds.Quests[1].Id, Done(worlds.Quests[0].Id), Captured));
        // nor a quest of a campaign that is not running
        Assert.Null(SeasonalQuestRules.Takeable(campaigns, worlds.Quests[0].Id, NoProgress, worlds.EndsAt));
    }

    [Fact]
    public async Task AWearBadgeQuestCountsItsOwnBadgeAndIsDoneAtItsSteps()
    {
        var quest = (await SeedAsync()).Single(c => c.Code == "bawcollab_worlds").Quests[0];
        Assert.True(quest.Counts(SeasonalQuestTypes.WearBadge, "W2601"));
        Assert.False(quest.Counts(SeasonalQuestTypes.WearBadge, "W2602"));
        Assert.False(quest.Counts("WAVE", "W2601"));

        var progress = new UserSeasonalQuest { QuestId = quest.Id, Accepted = true };
        Assert.True(SeasonalQuestRules.Count(quest, progress, 5, Captured));
        Assert.Equal((1, false, Captured), (progress.Progress, progress.Accepted, progress.CompletedAt));
        Assert.False(SeasonalQuestRules.Pays(quest));
    }
}
