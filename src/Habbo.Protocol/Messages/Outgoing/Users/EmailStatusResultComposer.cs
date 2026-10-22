using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>2549 EmailStatusResultComposer.</summary>
[Message(ServerMessages.EmailStatusResultComposer)]
public sealed partial record EmailStatusResultComposer(string Email, bool IsVerified, bool AllowChange);
