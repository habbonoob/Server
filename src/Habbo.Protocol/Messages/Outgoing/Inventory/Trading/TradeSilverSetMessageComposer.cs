using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Trading;

/// <summary>TradeSilverSetMessageComposer</summary>
[Message(ServerMessages.TradeSilverSetMessageComposer)]
public sealed partial record TradeSilverSetMessageComposer(int PlayerSilver, int OtherPlayerSilver);
