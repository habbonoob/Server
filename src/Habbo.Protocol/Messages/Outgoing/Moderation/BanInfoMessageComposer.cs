using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Moderation;

/// <summary>BanInfoMessageComposer</summary>
[Message(ServerMessages.BanInfoMessageComposer)]
public sealed partial record BanInfoMessageComposer([property: WireFormat(WireType.I16)] short Target, string Reason, int BanExpirySeconds, string LocalizedReason);
