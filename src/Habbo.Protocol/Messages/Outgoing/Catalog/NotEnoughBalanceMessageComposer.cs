using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>NotEnoughBalanceMessageComposer — the purchase was short of credits or of activity points of a kind</summary>
[Message(ServerMessages.NotEnoughBalanceMessageComposer)]
public sealed partial record NotEnoughBalanceMessageComposer(bool NotEnoughCredits, bool NotEnoughActivityPoints, int? ActivityPointType = null);
