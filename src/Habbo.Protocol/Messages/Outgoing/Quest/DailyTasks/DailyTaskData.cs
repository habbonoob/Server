using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Quest.DailyTasks;

/// <summary>A daily task as the client lists it: its id, its code (which keys its texts and image), what it counts
/// (the client reads nothing from this), whether it is the bonus task, its image's version, a catalogue page, the
/// repeats it takes and has had, its status (0 in progress, 1 done, 2 claimed), the seconds until its day ends (below
/// zero for a done one from an earlier day) and its rewards.</summary>
[WireStruct]
public sealed partial record DailyTaskData(
    long TaskId,
    string TaskCode,
    string QuestTypeCode,
    bool IsBonus,
    string ImageVersion,
    string CatalogName,
    int RequiredRepeats,
    int Repeats,
    byte Status,
    int SecondsLeft,
    IReadOnlyList<DailyTaskRewardData> Rewards);

/// <summary>A daily task's reward: a client product type (8 currency), the item (the currency's type), its extra
/// parameters and how many.</summary>
[WireStruct]
public sealed partial record DailyTaskRewardData(
    [property: WireFormat(WireType.I16)] int ProductType,
    string RewardTypeId,
    string ExtraParams,
    int Amount);
