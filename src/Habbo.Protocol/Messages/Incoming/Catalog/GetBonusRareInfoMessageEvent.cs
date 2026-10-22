using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>GetBonusRareInfoMessageEvent</summary>
[Incoming(ClientMessages.GetBonusRareInfoMessageEvent)]
public sealed partial record GetBonusRareInfoMessageEvent;
