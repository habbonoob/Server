using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.PeerService;

/// <summary>ClaimProductMessageEvent — claim a free product, and which of its choices when it offers some (sent only then:
/// the cursed dragon adoption, 0 blue, 1 purple).</summary>
[Incoming(ClientMessages.ClaimProductMessageEvent)]
public sealed partial record ClaimProductMessageEvent(string ClaimId, int? Choice = null);
