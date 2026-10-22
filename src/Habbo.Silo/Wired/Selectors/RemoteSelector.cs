using Habbo.Silo.Abstractions.Wired;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired.Selectors;

/// <summary>Chooses what the selectors of other wired stacks choose: the stacks of the boxes it
/// picks — all of them, or so many at random — joined as a union or an intersection. A stack that
/// reaches back to one already being selected for adds nothing.</summary>
[WiredBox("wf_slc_remote")]
public sealed class RemoteSelector : WiredBoxType
{
    // remote_selection.type.0 and .1
    private const int Union = 0;
    private const int Intersection = 1;

    private const int Joining = 0;
    private const int RandomAmount = 1;

    public override WiredKind Kind => WiredKind.Selector;
    public override int Code => SelectorCodes.RemoteSelector;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.SelectorFurni];
    public override IReadOnlyList<int> DefaultIntParams => [Union, 0];

    public static (List<RoomItemStore.PlacedItem> Furni, List<RoomEntityStore.RoomEntity> Users) Select(WiredBox box, WiredRun run)
    {
        run.VisitedStacks.Add((box.Item.X, box.Item.Y));
        var stacks = run.Furni(box)
            .Select(item => (item.X, item.Y))
            .Distinct()
            .Where(tile => !run.VisitedStacks.Contains(tile))
            .Select(tile => run.Room.StackAt(tile.X, tile.Y))
            .OfType<IReadOnlyList<WiredBox>>()
            .ToList();

        var amount = box.IntParam(RandomAmount);
        if (amount > 0)
            stacks = stacks.OrderBy(_ => run.Room.Random.Next()).Take(amount).ToList();

        List<RoomItemStore.PlacedItem>? furni = null;
        List<RoomEntityStore.RoomEntity>? users = null;
        foreach (var stack in stacks)
        {
            var remote = new WiredRun(run.Room, run.Event) { VisitedStacks = run.VisitedStacks, Stack = stack, Execution = run.Execution };
            WiredSelection.Run(stack, remote);
            furni = Join(box, furni, remote.SelectedFurni);
            users = Join(box, users, remote.SelectedUsers);
        }

        return (furni ?? [], users ?? []);
    }

    private static List<T> Join<T>(WiredBox box, List<T>? sofar, List<T> more) =>
        sofar is null ? more.ToList()
        : box.IntParam(Joining) == Intersection ? sofar.Intersect(more).ToList()
        : sofar.Union(more).ToList();
}
