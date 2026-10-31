using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Quest;

/// <summary>A quest as the quest engine shows it: its campaign and how far the player is through it, the quest
/// itself (an id below 1 when the campaign is done), its reward in activity points (-1 for none), its steps, and
/// for a seasonal one how long it has left. Texts are quests.&lt;campaign&gt;.&lt;localization code&gt;.*; the icon is
/// &lt;campaign&gt;_&lt;localization code&gt;&lt;image version&gt;. A calendar day's sort order is the day.</summary>
[WireStruct]
public sealed partial record QuestMessageData(
    string CampaignCode,
    int CompletedQuestsInCampaign,
    int QuestCountInCampaign,
    int ActivityPointType,
    int Id,
    bool Accepted,
    string Type,
    string ImageVersion,
    int RewardCurrencyAmount,
    string LocalizationCode,
    int CompletedSteps,
    int TotalSteps,
    int SortOrder,
    string CatalogPageName,
    string ChainCode,
    bool Easy,
    bool IsSeasonal,
    /// <summary>Seconds until the seasonal campaign ends.</summary>
    [property: WireWhen("IsSeasonal")] int? SecondsLeft);
