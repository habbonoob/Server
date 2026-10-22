using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.PeerService;

/// <summary>ClaimProductMessageEvent — claim a free product</summary>
[Incoming(ClientMessages.ClaimProductMessageEvent)]
public sealed partial record ClaimProductMessageEvent(string ClaimId);
