using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Arena;

/// <summary>Game2PlayerRematchesMessageComposer — a player wants to play again</summary>
[Message(ServerMessages.Game2PlayerRematchesMessageComposer)]
public sealed partial record Game2PlayerRematchesMessageComposer(int UserId);
