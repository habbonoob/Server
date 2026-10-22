using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Directory;

/// <summary>Game2GameDirectoryStatusMessageComposer — status 0: open; the rest as the account's</summary>
[Message(ServerMessages.Game2GameDirectoryStatusMessageComposer)]
public sealed partial record Game2GameDirectoryStatusMessageComposer(int Status, int BlockLength, int GamesPlayed, int FreeGamesLeft);
