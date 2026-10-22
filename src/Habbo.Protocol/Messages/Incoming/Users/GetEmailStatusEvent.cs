using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>1000 GetEmailStatus.</summary>
[Incoming(ClientMessages.GetEmailStatusEvent)]
public sealed partial record GetEmailStatusEvent;
