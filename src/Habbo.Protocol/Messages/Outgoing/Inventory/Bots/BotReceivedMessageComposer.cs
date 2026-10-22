using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Bots;

/// <summary>BotReceivedMessageComposer — a bot was bought or given</summary>
[Message(ServerMessages.BotReceivedMessageComposer)]
public sealed partial record BotReceivedMessageComposer(bool BoughtAsGift, BotData Bot);
