using Habbo.Infrastructure;

namespace Habbo.Silo.Quests;

/// <summary>The seasonal campaigns, read from seed/seasonal_quests.json once: each calendar laid out as a campaign a
/// day, and the time-limited campaigns as they are.</summary>
public sealed class SeasonalQuestCatalog(SeedFiles seedFiles)
{
    private const string SeedFileName = "seasonal_quests.json";

    private IReadOnlyList<SeasonalCampaign>? _campaigns;
    private readonly SemaphoreSlim _loading = new(1, 1);

    public async Task<IReadOnlyList<SeasonalCampaign>> AllAsync()
    {
        if (_campaigns is not null)
            return _campaigns;

        await _loading.WaitAsync();
        try
        {
            return _campaigns ??= From(await seedFiles.ReadJsonAsync<SeedFile>(SeedFileName));
        }
        finally
        {
            _loading.Release();
        }
    }

    /// <summary>A calendar's day d opens at the start (UTC) of the d-th day from its first, its quest's sort order the day;
    /// every day stays open until the calendar's last day is over. A campaign's quests are sorted in their order.</summary>
    public static IReadOnlyList<SeasonalCampaign> From(SeedFile? file)
    {
        var campaigns = new List<SeasonalCampaign>();
        foreach (var calendar in file?.Calendars ?? [])
        {
            var endsAt = AtMidnight(calendar.EndsOn.AddDays(1));
            foreach (var day in calendar.Days ?? [])
            {
                campaigns.Add(new SeasonalCampaign(day.Campaign, day.Chain ?? day.Campaign,
                    AtMidnight(calendar.StartsOn.AddDays(day.Day - 1)), endsAt,
                    [QuestOf(day.Quest, day.Day, calendar.Reward, calendar.ActivityPointType)]));
            }
        }

        foreach (var campaign in file?.Campaigns ?? [])
        {
            var quests = campaign.Quests ?? [];
            campaigns.Add(new SeasonalCampaign(campaign.Code, campaign.Chain ?? campaign.Code, campaign.StartsAt, campaign.EndsAt,
                quests.Select((q, i) => QuestOf(q, i + 1, campaign.Reward, campaign.ActivityPointType)).ToList()));
        }

        return campaigns;
    }

    private static SeasonalQuest QuestOf(SeedQuest quest, int sortOrder, int reward, int activityPointType) =>
        new(quest.Id, quest.Code, quest.Type, quest.Parameter ?? string.Empty, Math.Max(1, quest.Steps ?? 1),
            quest.Reward ?? reward, activityPointType, quest.ImageVersion ?? string.Empty, quest.CatalogPage ?? string.Empty,
            quest.Easy, sortOrder);

    private static DateTimeOffset AtMidnight(DateOnly day) => new(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    public sealed record SeedFile(List<SeedCalendar>? Calendars, List<SeedCampaign>? Campaigns);

    /// <summary>An advent calendar: the client draws the campaigns its prefix starts, a day each.</summary>
    public sealed record SeedCalendar(string Prefix, DateOnly StartsOn, DateOnly EndsOn, int ActivityPointType, int Reward, List<SeedDay>? Days);

    public sealed record SeedDay(int Day, string Campaign, string? Chain, SeedQuest Quest);

    public sealed record SeedCampaign(string Code, string? Chain, DateTimeOffset StartsAt, DateTimeOffset EndsAt, int ActivityPointType,
        int Reward, List<SeedQuest>? Quests);

    /// <summary>A quest: what it counts (a type, and a parameter narrowing it when not empty, such as WEAR_BADGE's badge
    /// code), how many times, and what it pays when that is not its campaign's.</summary>
    public sealed record SeedQuest(int Id, string Code, string Type, string? Parameter, int? Steps, int? Reward, string? ImageVersion,
        string? CatalogPage, bool Easy);
}

/// <summary>A seasonal campaign: a chain of quests, taken on one after another, open from its start until its end.</summary>
public sealed record SeasonalCampaign(string Code, string ChainCode, DateTimeOffset StartsAt, DateTimeOffset EndsAt, IReadOnlyList<SeasonalQuest> Quests)
{
    public bool IsRunning(DateTimeOffset now) => StartsAt <= now && now < EndsAt;
}

/// <summary>A quest of a seasonal campaign: its id, its texts' code (quests.&lt;campaign&gt;.&lt;code&gt;.*), what it counts and
/// how many times, and its reward in activity points (none below 0 or at 0).</summary>
public sealed record SeasonalQuest(
    int Id,
    string LocalizationCode,
    string Type,
    string Parameter,
    int Steps,
    int Reward,
    int ActivityPointType,
    string ImageVersion,
    string CatalogPage,
    bool Easy,
    int SortOrder)
{
    public bool Counts(string type, string parameter) => Type == type && (Parameter.Length == 0 || Parameter == parameter);
}
