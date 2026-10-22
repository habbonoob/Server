using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Directory;

/// <summary>Game2AccountGameStatusMessageComposer — free games left (-1: unlimited) and games played</summary>
[Message(ServerMessages.Game2AccountGameStatusMessageComposer)]
public sealed partial record Game2AccountGameStatusMessageComposer(int GameTypeId, int FreeGamesLeft, int GamesPlayedTotal);
