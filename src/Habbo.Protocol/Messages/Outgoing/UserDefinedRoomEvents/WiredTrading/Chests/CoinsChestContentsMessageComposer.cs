using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>CoinsChestContentsMessageComposer — the credits in a chest</summary>
[Message(ServerMessages.CoinsChestContentsMessageComposer)]
public sealed partial record CoinsChestContentsMessageComposer(int ChestId, int Coins, bool IsUpdate);
