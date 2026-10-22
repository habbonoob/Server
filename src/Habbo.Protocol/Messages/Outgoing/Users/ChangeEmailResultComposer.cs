using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>529 ChangeEmailResultComposer.</summary>
[Message(ServerMessages.ChangeEmailResultComposer)]
public sealed partial record ChangeEmailResultComposer(int Result);
