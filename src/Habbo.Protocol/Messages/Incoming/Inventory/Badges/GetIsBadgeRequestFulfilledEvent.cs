using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Inventory.Badges;

/// <summary>3835 GetIsBadgeRequestFulfilled — whether the player already has what a code gives.</summary>
[Incoming(ClientMessages.GetIsBadgeRequestFulfilledEvent)]
public sealed partial record GetIsBadgeRequestFulfilledEvent(string RequestCode);
