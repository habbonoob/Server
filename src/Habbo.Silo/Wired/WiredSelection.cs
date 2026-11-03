using Habbo.Domain.Furniture.Data;
using Habbo.Silo.Wired.Selectors;

namespace Habbo.Silo.Wired;

/// <summary>Runs a stack's selectors: first those that add what they choose to what the stack works on, lowest
/// first, then the filters, which keep only what they also choose; inverted, a selector chooses everything it would
/// not. A filter narrows what the others chose wherever it stands in the stack: habbo.com, October 2026, a Furni In
/// Area filter under the Furni Picks selector it filters still kept only the picks in its area.</summary>
public static class WiredSelection
{
    public static void Run(IReadOnlyList<WiredBox> stack, WiredRun run)
    {
        foreach (var box in stack.Where(box => !box.Settings.IsFilter).Concat(stack.Where(box => box.Settings.IsFilter)))
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
