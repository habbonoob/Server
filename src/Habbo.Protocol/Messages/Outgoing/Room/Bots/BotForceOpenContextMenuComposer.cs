using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Bots;

/// <summary>BotForceOpenContextMenuComposer — open a bot's menu for the player</summary>
[Message(ServerMessages.BotForceOpenContextMenuComposer)]
public sealed partial record BotForceOpenContextMenuComposer(int BotId);
