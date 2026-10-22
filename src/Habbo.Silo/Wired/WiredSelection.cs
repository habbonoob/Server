using Habbo.Domain.Furniture.Data;
using Habbo.Silo.Wired.Selectors;

namespace Habbo.Silo.Wired;

/// <summary>Runs a stack's selectors, lowest first, each adding to what the stack works on — or,
/// as a filter, keeping only what it also chooses; inverted, choosing everything it would not.</summary>
public static class WiredSelection
{
    public static void Run(IReadOnlyList<WiredBox> stack, WiredRun run)
    {
        foreach (var box in stack)
        {
            switch (box.Type)
            {
                case WiredFurniSelector selector:
                    run.SelectedFurni = Combine(run.SelectedFurni, selector.Select(box, run), run.Room.Items.Items, box.Settings);
                    break;
                case WiredUserSelector selector:
                    run.SelectedUsers = Combine(run.SelectedUsers, selector.Select(box, run), run.Room.Entities.Entities, box.Settings);
                    break;
                case RemoteSelector:
                    var (furni, users) = RemoteSelector.Select(box, run);
                    run.SelectedFurni = Combine(run.SelectedFurni, furni, run.Room.Items.Items, box.Settings);
                    run.SelectedUsers = Combine(run.SelectedUsers, users, run.Room.Entities.Entities, box.Settings);
                    break;
            }
        }
    }

    private static List<T> Combine<T>(List<T> current, IEnumerable<T> chosen, IEnumerable<T> everything, FurniWired settings)
    {
        var chosenSet = chosen.ToHashSet();
        var picked = settings.IsInvert ? everything.Where(x => !chosenSet.Contains(x)).ToHashSet() : chosenSet;
        return settings.IsFilter ? current.Where(picked.Contains).ToList() : current.Union(picked).ToList();
    }
}
