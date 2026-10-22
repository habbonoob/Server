namespace Habbo.Domain.Rooms.Copies;

/// <summary>A wired variable of a copied room, as the hotel listed it to the client. Habbo numbers the room's own
/// variables apart from the furni (and its built-in ones below zero), so the import finds each by its name: the
/// variable box that made it, a built-in of the same name, or the part of one of those a sub-variable names.</summary>
/// <param name="Id">Its id there, which wired boxes use it by (<see cref="RoomCopyWired.VariableIds"/>).</param>
/// <param name="Kind">Made by a variable box (0), built in (1), part of another (2), or smart (3).</param>
/// <param name="Name">Its name: the variable box's text, or "@name" for a built-in, "parent.part" for a part.</param>
/// <param name="Target">What holds it: furni (0), users (1), the room (-10), a stack's run (-20).</param>
public sealed record RoomCopyVariable(string Id, int Kind, string Name, int Target);
