using Habbo.Protocol.Wire;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;

/// <summary>One entry of a wired dialog's context (the client's WiredContext): its type, then that
/// type's data — the room's variables' hash (which the client syncs its list by), a furni or user
/// variable with its holders, the room variable with its value, variables and placeholders shared
/// from other rooms, or a list of variables.</summary>
[WireStruct]
public sealed partial record WiredContextEntryData(
    int Type,
    [property: WireWhen("IsAllVariables")] int? AllVariablesHash,
    [property: WireWhen("CarriesVariable")] WiredVariableData? Variable,
    [property: WireWhen("CarriesHolders")] IReadOnlyList<WiredVariableHolderData>? Holders,
    [property: WireWhen("IsRoomVariable")] int? Value,
    [property: WireWhen("IsSharedVariables")] IReadOnlyList<WiredSharedVariableData>? SharedVariables,
    [property: WireWhen("IsVariableList")] IReadOnlyList<WiredVariableData>? Variables,
    [property: WireWhen("IsSharedPlaceholders")] IReadOnlyList<WiredSharedPlaceholderData>? SharedPlaceholders)
{
    // WiredContextType
    public const int AllVariablesType = 0;
    public const int FurniVariableType = 1;
    public const int UserVariableType = 2;
    public const int RoomVariableType = 3;
    public const int SharedVariablesType = 4;
    public const int VariableListType = 5;
    public const int SharedPlaceholdersType = 6;

    public bool IsAllVariables => Type == AllVariablesType;
    public bool CarriesVariable => Type is FurniVariableType or UserVariableType or RoomVariableType;
    public bool CarriesHolders => Type is FurniVariableType or UserVariableType;
    public bool IsRoomVariable => Type == RoomVariableType;
    public bool IsSharedVariables => Type == SharedVariablesType;
    public bool IsVariableList => Type == VariableListType;
    public bool IsSharedPlaceholders => Type == SharedPlaceholdersType;

    public static WiredContextEntryData AllVariables(int hash) =>
        new(AllVariablesType, hash, null, null, null, null, null, null);

    public static WiredContextEntryData Holding(int type, WiredVariableData variable, IReadOnlyList<WiredVariableHolderData> holders) =>
        new(type, null, variable, holders, null, null, null, null);

    public static WiredContextEntryData RoomVariable(WiredVariableData variable, int value) =>
        new(RoomVariableType, null, variable, null, value, null, null, null);

    public static WiredContextEntryData Shared(IReadOnlyList<WiredSharedVariableData> variables) =>
        new(SharedVariablesType, null, null, null, null, variables, null, null);

    public static WiredContextEntryData PlaceholdersShared(IReadOnlyList<WiredSharedPlaceholderData> placeholders) =>
        new(SharedPlaceholdersType, null, null, null, null, null, null, placeholders);
}
