namespace Habbo.Silo.Abstractions.Wired;

/// <summary>A change the wired menu makes to a held variable: set, give, or take (the client's actions).</summary>
public static class WiredVariableMenuActions
{
    public const int Set = 0;
    public const int Give = 1;
    public const int Remove = 2;
}

/// <summary>A request for a page of the users holding a permanent variable.</summary>
[GenerateSerializer]
public sealed partial record WiredOwnersQuery(
    [property: Id(0)] string VariableId,
    [property: Id(1)] int Page,
    [property: Id(2)] int PageSize,
    [property: Id(3)] int SortType,
    [property: Id(4)] int UserTypeFilter);

/// <summary>A change the wired menu makes to a variable some holder has.</summary>
[GenerateSerializer]
public sealed partial record WiredVariableEdit(
    [property: Id(0)] int Target,
    [property: Id(1)] int ObjectId,
    [property: Id(2)] string VariableId,
    [property: Id(3)] int Value,
    [property: Id(4)] int Action);

/// <summary>A value of a variable this room shares, changed from a room that references it: the
/// variable (its box's item id), whose value (a user; the room's own when none), and the value
/// now (none when it was taken).</summary>
[GenerateSerializer]
public sealed partial record SharedVariableChange(
    [property: Id(0)] int RoomId,
    [property: Id(1)] string VariableId,
    [property: Id(2)] Guid? UserId,
    [property: Id(3)] int? Value,
    [property: Id(4)] DateTimeOffset CreatedAt,
    [property: Id(5)] DateTimeOffset UpdatedAt);
