using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Game.Directory;

/// <summary>Game2CheckGameDirectoryStatusMessageEvent — whether the game directory is open, and how many games the player has</summary>
[Incoming(ClientMessages.Game2CheckGameDirectoryStatusMessageEvent)]
public sealed partial record Game2CheckGameDirectoryStatusMessageEvent;
