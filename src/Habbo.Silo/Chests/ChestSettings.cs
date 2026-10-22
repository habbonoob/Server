using System.Globalization;
using Habbo.Domain.Furniture.Data;
using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Silo.Chests;

/// <summary>A chest's settings and look, kept as the item's map data: the client reads every one of them from it.</summary>
public sealed record ChestSettings(
    string Name,
    string Description,
    bool EveryoneCanOpen,
    bool EveryoneCanDonate,
    int StateControlMode,
    int PreviewMode,
    int PreviewAmount,
    bool Locked,
    bool AutoLock,
    int? Capacity,
    int CapacityLevel,
    bool WiredEnabled,
    int NotifyMode,
    bool NotifyChestFull,
    bool NotifyDonation,
    bool NotifySomeoneWithdraws,
    bool NotifyChestEmpty,
    bool NotifyWiredTransaction,
    string Visuals,
    int State,
    int ContentsCount)
{
    /// <summary>How the chest's open state is decided (state_control_mode).</summary>
    public const int OpenWhenLookedInto = 0;
    public const int AlwaysOpen = 1;
    public const int AlwaysClosed = 2;
    public const int ControlledByWired = 3;

    /// <summary>Which items the open chest shows on top (preview_mode).</summary>
    public const int PreviewNone = 0;
    public const int PreviewRandom = 1;
    public const int PreviewRandomKinds = 2;
    public const int PreviewNewest = 3;
    public const int PreviewNewestKinds = 4;
    public const int PreviewOldest = 5;
    public const int PreviewOldestKinds = 6;
    public const int PreviewNextForWired = 7;

    /// <summary>When the owner is told (notify_mode).</summary>
    public const int NotifyAlways = 0;
    public const int NotifyWhenAway = 1;

    public const int NameMaxLength = 30;
    public const int DescriptionMaxLength = 200;
    public const int MinPreviewAmount = 1;
    public const int MaxPreviewAmount = 4;

    public const int ClosedState = 0;

    // ContentsCount (contents_count): the credits a coin chest holds, the items any other holds. The infostand shows it
    // ("Credits in Chest: 120"), and the inventory only stacks chests that hold nothing.

    /// <summary>What a new chest starts as: closed, showing nothing, with its capacity set once it is placed.</summary>
    public static ChestSettings Initial { get; } = new(
        string.Empty, string.Empty, false, false, OpenWhenLookedInto, PreviewNone, MinPreviewAmount, true, false, null, 0, false,
        NotifyAlways, false, false, false, false, false, string.Empty, ClosedState, 0);

    public static ChestSettings Read(FurniDataView data)
    {
        var map = (data.MapEntries ?? []).ToDictionary(e => e.Key, e => e.Value);
        string Text(string key) => map.GetValueOrDefault(key, string.Empty);
        bool Flag(string key, bool fallback) => map.TryGetValue(key, out var value) ? value == "1" : fallback;
        int Number(string key, int fallback) => int.TryParse(Text(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : fallback;

        var initial = Initial;
        return new ChestSettings(
            Text(NameKey), Text(DescriptionKey), Flag(EveryoneCanOpenKey, false), Flag(EveryoneCanDonateKey, false),
            Number(StateControlModeKey, initial.StateControlMode), Number(PreviewModeKey, initial.PreviewMode),
            Number(PreviewAmountKey, initial.PreviewAmount), Flag(LockedKey, initial.Locked), Flag(AutoLockKey, false),
            map.ContainsKey(CapacityKey) ? Number(CapacityKey, 0) : null, Number(CapacityLevelKey, 0), Flag(WiredEnabledKey, false),
            Number(NotifyModeKey, NotifyAlways), Flag(NotifyChestFullKey, false), Flag(NotifyDonationKey, false),
            Flag(NotifySomeoneWithdrawsKey, false), Flag(NotifyChestEmptyKey, false), Flag(NotifyWiredTransactionKey, false),
            Text(VisualsKey), Number(StateKey, ClosedState), Number(ContentsCountKey, 0));
    }

    public FurniDataView ToData()
    {
        static string Flag(bool value) => value ? "1" : "0";
        static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        List<FurniMapEntryView> entries =
        [
            new(NameKey, Name), new(DescriptionKey, Description), new(EveryoneCanOpenKey, Flag(EveryoneCanOpen)),
            new(EveryoneCanDonateKey, Flag(EveryoneCanDonate)), new(StateControlModeKey, Number(StateControlMode)),
            new(PreviewModeKey, Number(PreviewMode)), new(PreviewAmountKey, Number(PreviewAmount)), new(LockedKey, Flag(Locked)),
            new(AutoLockKey, Flag(AutoLock)), new(CapacityLevelKey, Number(CapacityLevel)), new(WiredEnabledKey, Flag(WiredEnabled)),
            new(NotifyModeKey, Number(NotifyMode)), new(NotifyChestFullKey, Flag(NotifyChestFull)), new(NotifyDonationKey, Flag(NotifyDonation)),
            new(NotifySomeoneWithdrawsKey, Flag(NotifySomeoneWithdraws)), new(NotifyChestEmptyKey, Flag(NotifyChestEmpty)),
            new(NotifyWiredTransactionKey, Flag(NotifyWiredTransaction)), new(VisualsKey, Visuals), new(StateKey, Number(State)),
            new(ContentsCountKey, Number(ContentsCount)),
        ];
        if (Capacity is { } capacity)
            entries.Add(new FurniMapEntryView(CapacityKey, Number(capacity)));

        return FurniDataView.Plain with { Type = (int)FurniDataType.Map, State = null, MapEntries = entries };
    }

    private const string NameKey = "chest_name";
    private const string DescriptionKey = "chest_desc";
    private const string EveryoneCanOpenKey = "everyone_can_open";
    private const string EveryoneCanDonateKey = "everyone_can_donate";
    private const string StateControlModeKey = "state_control_mode";
    private const string PreviewModeKey = "preview_mode";
    private const string PreviewAmountKey = "preview_amount";
    private const string LockedKey = "locked";
    private const string AutoLockKey = "auto_lock";
    private const string CapacityKey = "capacity";
    private const string CapacityLevelKey = "capacity_level";
    private const string WiredEnabledKey = "is_wired_enabled";
    private const string NotifyModeKey = "notify_mode";
    private const string NotifyChestFullKey = "notification_chest_full";
    private const string NotifyDonationKey = "notification_donation";
    private const string NotifySomeoneWithdrawsKey = "notification_someone_withdraws";
    private const string NotifyChestEmptyKey = "notification_chest_empty";
    private const string NotifyWiredTransactionKey = "notification_wired_transaction";
    private const string VisualsKey = "visuals";
    private const string StateKey = "state";
    private const string ContentsCountKey = "contents_count";
}
