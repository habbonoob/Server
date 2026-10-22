using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Game.Ingame;

/// <summary>Game2MakeSnowballMessageEvent — stop and make a snowball</summary>
[Incoming(ClientMessages.Game2MakeSnowballMessageEvent)]
public sealed partial record Game2MakeSnowballMessageEvent(int Turn, int Subturn);
