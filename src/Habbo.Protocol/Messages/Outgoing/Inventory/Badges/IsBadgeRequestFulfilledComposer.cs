using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Badges;

/// <summary>1368 IsBadgeRequestFulfilledComposer.</summary>
[Message(ServerMessages.IsBadgeRequestFulfilledComposer)]
public sealed partial record IsBadgeRequestFulfilledComposer(string RequestCode, bool Fulfilled);
