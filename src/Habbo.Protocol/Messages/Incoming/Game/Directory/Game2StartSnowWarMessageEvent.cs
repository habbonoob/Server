using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Game.Directory;

/// <summary>Game2StartSnowWarMessageEvent — start a game on a named arena</summary>
[Incoming(ClientMessages.Game2StartSnowWarMessageEvent)]
public sealed partial record Game2StartSnowWarMessageEvent(string LevelName);
