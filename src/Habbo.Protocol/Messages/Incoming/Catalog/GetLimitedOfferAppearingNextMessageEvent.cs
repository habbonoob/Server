using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>GetLimitedOfferAppearingNextMessageEvent</summary>
[Incoming(ClientMessages.GetLimitedOfferAppearingNextMessageEvent)]
public sealed partial record GetLimitedOfferAppearingNextMessageEvent;
