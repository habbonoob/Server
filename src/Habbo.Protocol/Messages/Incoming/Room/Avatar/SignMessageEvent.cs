using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Avatar;

/// <summary>1831 Sign — hold up one of the signs, 0 to 17.</summary>
[Incoming(ClientMessages.SignMessageEvent)]
public sealed partial record SignMessageEvent(int Sign);
