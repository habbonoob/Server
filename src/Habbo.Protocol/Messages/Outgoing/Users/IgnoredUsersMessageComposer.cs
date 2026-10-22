using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>782 IgnoredUsersMessageComposer — who the player has muted. The client hides their
/// chat; blocking is a separate list.</summary>
[Message(ServerMessages.IgnoredUsersMessageComposer)]
public sealed partial record IgnoredUsersMessageComposer(IReadOnlyList<int> UserIds);
