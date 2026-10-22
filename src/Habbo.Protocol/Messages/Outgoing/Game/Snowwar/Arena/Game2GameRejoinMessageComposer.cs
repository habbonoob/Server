using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Arena;

/// <summary>Game2GameRejoinMessageComposer — the player is back in the lobby after a game, and where they came from</summary>
[Message(ServerMessages.Game2GameRejoinMessageComposer)]
public sealed partial record Game2GameRejoinMessageComposer(int RoomBeforeGame);
