using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>1671 CreateFlat — the Create Room form.</summary>
[Incoming(ClientMessages.CreateFlatMessageEvent)]
public sealed partial record CreateFlatMessageEvent(string Name, string Description, string ModelName, int CategoryId, int MaxVisitors, int TradeMode);
