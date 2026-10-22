using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>203 IgnoreResultMessageComposer.</summary>
[Message(ServerMessages.IgnoreResultMessageComposer)]
public sealed partial record IgnoreResultMessageComposer(int Result, int IgnoredUserId);
