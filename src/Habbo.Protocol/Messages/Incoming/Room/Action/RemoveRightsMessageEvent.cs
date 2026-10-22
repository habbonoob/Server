using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Action;

/// <summary>2873 RemoveRights — the owner takes rights back from these users (by account id).</summary>
[Incoming(ClientMessages.RemoveRightsMessageEvent)]
public sealed partial record RemoveRightsMessageEvent(IReadOnlyList<int> UserIds);
