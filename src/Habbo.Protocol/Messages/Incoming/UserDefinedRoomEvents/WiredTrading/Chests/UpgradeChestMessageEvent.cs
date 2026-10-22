using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>UpgradeChestMessageEvent — buy more capacity for a chest</summary>
[Incoming(ClientMessages.UpgradeChestMessageEvent)]
public sealed partial record UpgradeChestMessageEvent(int ChestId, int Levels);
