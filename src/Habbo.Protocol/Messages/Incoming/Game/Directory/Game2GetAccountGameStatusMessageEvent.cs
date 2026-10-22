using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Game.Directory;

/// <summary>Game2GetAccountGameStatusMessageEvent — how many free games the player has left of a game</summary>
[Incoming(ClientMessages.Game2GetAccountGameStatusMessageEvent)]
public sealed partial record Game2GetAccountGameStatusMessageEvent(int GameTypeId);
