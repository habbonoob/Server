using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredMenu.VariablesManagement;

/// <summary>688 WiredGetVariableOwnersPage — a page of the users holding a permanent user variable,
/// sorted, and filtered by user type (-1 for any).</summary>
[Incoming(ClientMessages.WiredGetVariableOwnersPageEvent)]
public sealed partial record WiredGetVariableOwnersPageEvent(string VariableId, int Page, int PageSize, int SortType, int UserTypeFilter);
