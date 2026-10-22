using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired.Conditions;

/// <summary>The "require all / any" choice some conditions offer over their picked furni.</summary>
public static class WiredQuantity
{
    public const int All = 0;
    public const int Any = 1;

    /// <summary>Whether the test holds for all, or for any, of the items. No items holds for all. Each condition's own
    /// radio says which value means all: it differs from box to box (the client's requireall texts).</summary>
    public static bool Holds(bool all, IEnumerable<RoomItemStore.PlacedItem> items, Func<RoomItemStore.PlacedItem, bool> test) =>
        all ? items.All(test) : items.Any(test);

    /// <summary>A quantified condition over its targets: all or any of them match, as the box's
    /// quantifier says — and, for the negative box, the opposite ("any does not match", "none
    /// match"). No targets passes neither: there is nothing for the condition to be about. Except where Habbo measured
    /// otherwise (<paramref name="emptyByQuantifier"/>: Has Variable and Variable Value), where no targets passes "all"
    /// and fails "any".</summary>
    public static bool Passes<T>(WiredBox box, bool negative, IEnumerable<T> targets, Func<T, bool> matches,
        bool emptyByQuantifier = false)
    {
        var list = targets.ToList();
        if (list.Count == 0 && !emptyByQuantifier)
            return false;

        var holds = box.Settings.Quantifier == Any ? list.Any(matches) : list.All(matches);
        return holds != negative;
    }
}

/// <summary>What a condition's quantifier counts over, as the client names it
/// (wiredfurni.params.quantifier.furni/users/variables): none, when it has no quantifier.</summary>
public static class WiredQuantifierTypes
{
    public const byte None = 0;
    public const byte Furni = 1;
    public const byte Users = 2;
    public const byte Variables = 3;
}
