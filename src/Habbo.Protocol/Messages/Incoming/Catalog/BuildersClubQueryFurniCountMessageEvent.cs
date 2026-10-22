using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>3669 BuildersClubQueryFurniCount — how many Builders Club items the player has out; the warehouse
/// asks when it opens and counts its limit against the answer.</summary>
[Incoming(ClientMessages.BuildersClubQueryFurniCountMessageEvent)]
public sealed partial record BuildersClubQueryFurniCountMessageEvent;
