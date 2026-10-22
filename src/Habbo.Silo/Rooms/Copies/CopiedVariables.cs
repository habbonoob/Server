using System.Globalization;
using Habbo.Domain.Rooms.Copies;
using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Rooms.Copies;

/// <summary>The ids a copied room's variables get here. Habbo numbers a room's own variables apart from its furni and
/// its built-in ones below zero, so each is found by its name: the variable box of that name (names are unique in a
/// room), the internal variable of that name and holder, or, for a part of a variable ("parent.part"), that part of
/// what its parent became. A variable none of these finds is left out.</summary>
public static class CopiedVariables
{
    /// <param name="variables">The room's variables as the hotel listed them.</param>
    /// <param name="boxesByName">The new ids of the variable boxes imported, by the names they give their variables.</param>
    /// <returns>The new id of each variable found, by its id there.</returns>
    public static IReadOnlyDictionary<string, string> Map(
        IReadOnlyList<RoomCopyVariable> variables, IReadOnlyDictionary<string, int> boxesByName)
    {
        var boxes = new Dictionary<string, int>(boxesByName, StringComparer.OrdinalIgnoreCase);
        var map = new Dictionary<string, string>();

        foreach (var variable in variables.Where(v => v.Kind != WiredVariableKinds.SubVariable))
        {
            if (Own(variable, boxes) is { } id)
                map.TryAdd(variable.Id, id);
        }

        foreach (var part in variables.Where(v => v.Kind == WiredVariableKinds.SubVariable))
        {
            var separator = part.Name.IndexOf(WiredSubVariables.Separator);
            if (separator <= 0)
                continue;

            var parentName = part.Name[..separator];
            var parent = variables.FirstOrDefault(v => v.Kind != WiredVariableKinds.SubVariable && v.Target == part.Target
                && string.Equals(v.Name, parentName, StringComparison.OrdinalIgnoreCase));
            if (parent is not null && map.TryGetValue(parent.Id, out var parentId))
                map.TryAdd(part.Id, parentId + part.Name[separator..]);
        }

        return map;
    }

    private static string? Own(RoomCopyVariable variable, Dictionary<string, int> boxes)
    {
        if (variable.Kind == WiredVariableKinds.Internal)
        {
            var id = WiredInternalVariables.IdOf(variable.Target, variable.Name);
            return WiredInternalVariables.Find(id) is not null ? id : null;
        }

        return boxes.TryGetValue(variable.Name, out var box) ? box.ToString(CultureInfo.InvariantCulture) : null;
    }
}
