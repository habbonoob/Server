using Habbo.Domain.Furniture.Data;
using Habbo.Silo.Abstractions.Wired;

namespace Habbo.Silo.Wired;

/// <summary>What one kind of wired box is: its dialog — the client's layout code, what the player
/// may pick — and what it does. One instance serves every box of its kind in every room, so a
/// box's own settings and running state live on its <see cref="WiredBox"/>, never here.</summary>
public abstract class WiredBoxType
{
    /// <summary>How many furni one box may pick.</summary>
    public const int MaxPickedFurni = 20;

    /// <summary>The most int parameters a dialog sends; more is not a dialog talking. The largest Habbo's dialogs send
    /// (bc_run4) is the levelling display add-on's 22; the neighbourhood's is 18 (three, then its drawing in 15 words).</summary>
    public const int MaxIntParams = 22;

    public abstract WiredKind Kind { get; }

    /// <summary>The client's number for the box's dialog layout (TriggerConfCodes, ActionTypeCodes,
    /// ConditionCodes, SelectorCodes, AddonCodes).</summary>
    public abstract int Code { get; }

    /// <summary>Whether the box works on furni, taken from wherever its dialog's furni source says:
    /// the picks, the selectors, a signal, and so on. Shorthand for one standard furni slot.</summary>
    public virtual bool PicksFurni => false;

    /// <summary>Whether the box's picks may include wall items. Habbo's dialogs say so per box (their
    /// allowWallFurni): those about states, variables, names and chests do, those that walk on furni don't.</summary>
    public virtual bool AllowsWallFurni => false;

    /// <summary>Whether the box works on users, taken from wherever its dialog's user source says.
    /// Shorthand for one standard user slot.</summary>
    public virtual bool TargetsUsers => false;

    /// <summary>The dialog's furni slots, each with the sources it offers.</summary>
    public virtual IReadOnlyList<WiredSlot> FurniSlots =>
        !PicksFurni ? []
        // effects and conditions work on furni from anywhere; triggers listen to their picks or their selectors'; the
        // rest read their own picks
        : Kind is WiredKind.Effect or WiredKind.Condition ? [WiredSlot.Furni]
        : Kind is WiredKind.Trigger ? [WiredSlot.TriggerFurni]
        : [WiredSlot.PickedOnly];

    /// <summary>The dialog's user slots, each with the sources it offers.</summary>
    public virtual IReadOnlyList<WiredSlot> UserSlots => TargetsUsers ? [WiredSlot.Users] : [];

    /// <summary>What the box's picks must be, when not just any furni: counters, antennas.</summary>
    public virtual WiredPickRule? PickRule => null;

    /// <summary>Whether the box keeps picks at all: some slot offers them (or, for the picks selector, which has no
    /// slot, the box is its picks).</summary>
    public virtual bool TakesPicks => FurniSlots.Any(slot =>
        slot.Sources.Contains(WiredSources.PickedFurni) || slot.Sources.Contains(WiredSources.PickedFurni2));

    /// <summary>The int parameters a new box starts with, and the dialog resets to.</summary>
    public virtual IReadOnlyList<int> DefaultIntParams => [];

    /// <summary>What the dialog's reset goes back to, which Habbo sends apart from a new box's parameters: for many
    /// boxes nothing, leaving the reset to the client.</summary>
    public virtual IReadOnlyList<int> ResetIntParams => [];

    /// <summary>Whether saving records the picked furni as they stand, for the boxes that match
    /// or restore that picture.</summary>
    public virtual bool TakesSnapshot => false;

    /// <summary>The permission saving the box needs on top of the room's wired rights, for a box that hands
    /// out what is worth something; null for most.</summary>
    public virtual string? Permission => null;

    /// <summary>Checks a box's settings before they are saved: null when they are fine, otherwise
    /// the text key the client shows (with its parameters). The room has already checked what
    /// every box shares — the picks, the text length.</summary>
    public virtual WiredValidation? Validate(FurniWired settings) => null;

    /// <summary>Checks what only the hotel can tell about the settings, after <see cref="Validate"/> passed them.</summary>
    public virtual Task<WiredValidation?> ValidateAsync(FurniWired settings, IWiredRoom room) => Task.FromResult<WiredValidation?>(null);

    /// <summary>Why the settings may not be saved for this box where it stands, among the stack's other boxes.</summary>
    public virtual WiredValidation? ValidateInStack(FurniWired settings, WiredBox self, IReadOnlyList<WiredBox> stack) => null;
}

/// <summary>Starts its stack when <see cref="Matches"/> says the event is the one it waits for.</summary>
public abstract class WiredTrigger : WiredBoxType
{
    /// <summary>Whether the trigger listens to the item: one of its picks, or of what its stack's selectors choose for
    /// this event, or of the furni the signal carries, as its furni source says.</summary>
    protected static bool Watches(WiredBox box, Rooms.RoomItemStore.PlacedItem item, WiredEvent e, IWiredRoom room) => box.FurniSource() switch
    {
        WiredSources.SelectedFurni => room.SelectedFor(box, e).Furni.Contains(item),
        WiredSources.SignalFurni => e.SignalFurni.Contains(item),
        _ => box.Settings.StuffIds.Contains(item.Item.Id),
    };

    public override WiredKind Kind => WiredKind.Trigger;

    public abstract bool Matches(WiredBox box, WiredEvent e, IWiredRoom room);
}

/// <summary>Runs when its stack does, after its delay — or, for a negative effect, when its stack's
/// conditions fail instead.</summary>
public abstract class WiredEffect : WiredBoxType
{
    public override WiredKind Kind => WiredKind.Effect;

    /// <summary>A negative effect: the stack's "else", which runs only when its conditions fail.</summary>
    public virtual bool IsNegative => false;

    public abstract Task RunAsync(WiredBox box, WiredRun run);
}

/// <summary>Lets its stack's effects run only when it passes.</summary>
public abstract class WiredCondition : WiredBoxType
{
    public override WiredKind Kind => WiredKind.Condition;

    /// <summary>A negative condition: it opens its positive's dialog (under a code of its own), marked inverted, and
    /// passes when the positive would not.</summary>
    public virtual bool IsNegative => false;

    /// <summary>Whether the dialog shows the condition inverted: the negatives, and those Habbo sends so whose own
    /// dialog says the negative already.</summary>
    public virtual bool InvertedInDialog => IsNegative;

    /// <summary>What the dialog's "all / any" choice counts over (a <see cref="Conditions.WiredQuantifierTypes"/>
    /// value); none for a condition with no such choice.</summary>
    public virtual byte QuantifierType => Conditions.WiredQuantifierTypes.None;

    public abstract bool Passes(WiredBox box, WiredRun run);
}

/// <summary>A condition that tests each of its users: all or any of them must match, as the
/// dialog's quantifier chooses.</summary>
public abstract class WiredUserCondition : WiredCondition
{
    public override bool TargetsUsers => true;
    public override byte QuantifierType => Conditions.WiredQuantifierTypes.Users;

    public abstract bool Matches(Rooms.RoomEntityStore.RoomEntity user, WiredBox box, WiredRun run);

    public override bool Passes(WiredBox box, WiredRun run) =>
        Conditions.WiredQuantity.Passes(box, IsNegative, run.Users(box), user => Matches(user, box, run));
}

/// <summary>A condition that tests each of its furni: all or any of them must match, as the
/// dialog's quantifier chooses.</summary>
public abstract class WiredFurniCondition : WiredCondition
{
    public override bool PicksFurni => true;
    public override byte QuantifierType => Conditions.WiredQuantifierTypes.Furni;

    public abstract bool Matches(Rooms.RoomItemStore.PlacedItem item, WiredBox box, WiredRun run);

    public override bool Passes(WiredBox box, WiredRun run) =>
        Conditions.WiredQuantity.Passes(box, IsNegative, run.Furni(box), item => Matches(item, box, run));
}

/// <summary>Chooses furni for its stack to work on, before conditions and effects.</summary>
public abstract class WiredFurniSelector : WiredBoxType
{
    public override WiredKind Kind => WiredKind.Selector;

    public abstract IEnumerable<Rooms.RoomItemStore.PlacedItem> Select(WiredBox box, WiredRun run);
}

/// <summary>Chooses users for its stack to work on, before conditions and effects.</summary>
public abstract class WiredUserSelector : WiredBoxType
{
    public override WiredKind Kind => WiredKind.Selector;

    public abstract IEnumerable<Rooms.RoomEntityStore.RoomEntity> Select(WiredBox box, WiredRun run);
}

/// <summary>Changes how its stack runs. The room reads an add-on's presence and settings while it
/// runs the stack; the add-on itself does nothing.</summary>
public abstract class WiredAddon : WiredBoxType
{
    public override WiredKind Kind => WiredKind.Addon;
}

/// <summary>What a box's picks must be, and the client text that says so when they are not.</summary>
public sealed record WiredPickRule(Func<Rooms.RoomItemStore.PlacedItem, bool> Accepts, string ErrorKey);

/// <summary>Why settings were refused: the client text to show, and the values put into it.</summary>
public sealed record WiredValidation(string LocalizationKey, IReadOnlyDictionary<string, string> Parameters)
{
    public static WiredValidation Of(string localizationKey) =>
        new(localizationKey, new Dictionary<string, string>());
}

/// <summary>Registers a box type for the item class names it serves, as
/// <see cref="Habbo.Silo.Items.Interactions.FurniInteractionAttribute"/> does for behaviours.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class WiredBoxAttribute(params string[] classNames) : Attribute
{
    public IReadOnlyList<string> ClassNames { get; } = classNames;
}
