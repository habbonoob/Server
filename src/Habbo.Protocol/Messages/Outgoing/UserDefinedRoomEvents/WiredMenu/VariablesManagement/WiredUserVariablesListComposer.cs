using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu.VariablesManagement;

/// <summary>2104 WiredUserVariablesListComposer — a page of the users holding a permanent variable.</summary>
[Message(ServerMessages.WiredUserVariablesListComposer)]
public sealed partial record WiredUserVariablesListComposer(
    string VariableId,
    int TotalEntries,
    int Page,
    int PageSize,
    IReadOnlyList<WiredUserVariablesElementData> Elements,
    int UserTypeFilter,
    int SortType);
