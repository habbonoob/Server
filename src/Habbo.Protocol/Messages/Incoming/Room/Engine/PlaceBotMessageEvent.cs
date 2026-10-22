using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Engine;

/// <summary>PlaceBotMessageEvent — put a bot from the inventory down in the room</summary>
[Incoming(ClientMessages.PlaceBotMessageEvent)]
public sealed partial record PlaceBotMessageEvent(int BotId, int X, int Y);
