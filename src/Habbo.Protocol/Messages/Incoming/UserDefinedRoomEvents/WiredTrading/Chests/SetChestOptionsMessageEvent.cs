using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>SetChestOptionsMessageEvent — a chest's lock and capacity</summary>
[Incoming(ClientMessages.SetChestOptionsMessageEvent)]
public sealed partial record SetChestOptionsMessageEvent(int ChestId, bool Locked, bool AutoLock, int Capacity);
