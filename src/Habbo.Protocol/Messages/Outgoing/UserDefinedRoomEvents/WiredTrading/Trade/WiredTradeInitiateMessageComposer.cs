using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Trade.Requirements;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Trade;

/// <summary>WiredTradeInitiateMessageComposer — a wired trade begins</summary>
[Message(ServerMessages.WiredTradeInitiateMessageComposer)]
public sealed partial record WiredTradeInitiateMessageComposer(TradeRequirementData Requirement, bool ShowRequirementsImmediate, bool OverridePreviousTrade, int TimeoutSeconds);
