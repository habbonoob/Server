using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Habbicons;

/// <summary>HabbiconShopData (September 2026) — every collection and its Habbicons, as the player stands with them:
/// what the Habbicons Collection shows. Sent at login after UserHabbicons, and again whenever the client asks.</summary>
[Message(ServerMessages.HabbiconShopDataComposer)]
public sealed partial record HabbiconShopDataComposer(IReadOnlyList<HabbiconCollectionData> Collections);
