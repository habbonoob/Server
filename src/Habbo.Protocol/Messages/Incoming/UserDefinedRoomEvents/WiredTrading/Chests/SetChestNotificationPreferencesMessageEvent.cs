using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>SetChestNotificationPreferencesMessageEvent — what a chest's owner is told</summary>
[Incoming(ClientMessages.SetChestNotificationPreferencesMessageEvent)]
public sealed partial record SetChestNotificationPreferencesMessageEvent(int ChestId, int NotifyMode, bool ChestFull, bool Donation, bool SomeoneWithdraws, bool ChestEmpty, bool WiredTransaction);
