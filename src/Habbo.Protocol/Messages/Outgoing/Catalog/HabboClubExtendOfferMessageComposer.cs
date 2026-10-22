using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>HabboClubExtendOfferMessageComposer</summary>
[Message(ServerMessages.HabboClubExtendOfferMessageComposer)]
public sealed partial record HabboClubExtendOfferMessageComposer(ClubOfferExtendData Offer);
