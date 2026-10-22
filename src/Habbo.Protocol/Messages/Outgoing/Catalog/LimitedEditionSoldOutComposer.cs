using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>LimitedEditionSoldOutComposer — the limited edition sold out before the purchase</summary>
[Message(ServerMessages.LimitedEditionSoldOutComposer)]
public sealed partial record LimitedEditionSoldOutComposer;
