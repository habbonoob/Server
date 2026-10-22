using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Engine;

/// <summary>RemoveBotFromFlatMessageEvent — pick a bot up: it goes back to its owner</summary>
[Incoming(ClientMessages.RemoveBotFromFlatMessageEvent)]
public sealed partial record RemoveBotFromFlatMessageEvent(int BotId);
