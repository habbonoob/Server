using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>SelectClubGiftEvent — take one of the club's monthly gifts</summary>
[Incoming(ClientMessages.SelectClubGiftEvent)]
public sealed partial record SelectClubGiftEvent(string ProductCode);
