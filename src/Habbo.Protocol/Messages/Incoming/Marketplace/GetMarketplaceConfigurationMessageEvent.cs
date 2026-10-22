using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Marketplace;

/// <summary>GetMarketplaceConfigurationMessageEvent</summary>
[Incoming(ClientMessages.GetMarketplaceConfigurationMessageEvent)]
public sealed partial record GetMarketplaceConfigurationMessageEvent;
