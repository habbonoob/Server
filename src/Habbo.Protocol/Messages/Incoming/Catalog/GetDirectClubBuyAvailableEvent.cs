using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>GetDirectClubBuyAvailableEvent</summary>
[Incoming(ClientMessages.GetDirectClubBuyAvailableEvent)]
public sealed partial record GetDirectClubBuyAvailableEvent(int LengthInDays);
