using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>HabboClubOffersMessageComposer</summary>
[Message(ServerMessages.HabboClubOffersMessageComposer)]
public sealed partial record HabboClubOffersMessageComposer(IReadOnlyList<ClubOfferData> Offers, int Source);
