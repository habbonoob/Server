using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>3406 AccountSafetyLockStatusChangeMessageComposer.</summary>
[Message(ServerMessages.AccountSafetyLockStatusChangeMessageComposer)]
public sealed partial record AccountSafetyLockStatusChangeMessageComposer(int Status);
