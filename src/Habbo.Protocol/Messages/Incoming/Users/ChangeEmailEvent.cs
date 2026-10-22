using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>1173 ChangeEmail.</summary>
[Incoming(ClientMessages.ChangeEmailEvent)]
public sealed partial record ChangeEmailEvent(string Email);
