using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>GetClubOffersMessageEvent — the club's offers, for one of the client's places that sell it</summary>
[Incoming(ClientMessages.GetClubOffersMessageEvent)]
public sealed partial record GetClubOffersMessageEvent(int Source);
