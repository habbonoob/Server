using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Misc;

/// <summary>SelfDonateItemMessageEvent — give oneself so many of an item, in the sandbox</summary>
[Incoming(ClientMessages.SelfDonateItemMessageEvent)]
public sealed partial record SelfDonateItemMessageEvent(bool IsWallItem, int TypeId, string PosterId, int Amount);
