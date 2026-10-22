using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>UpgradeChestResultMessageComposer — how buying chest capacity went</summary>
[Message(ServerMessages.UpgradeChestResultMessageComposer)]
public sealed partial record UpgradeChestResultMessageComposer(int ChestId, int ResultCode);
