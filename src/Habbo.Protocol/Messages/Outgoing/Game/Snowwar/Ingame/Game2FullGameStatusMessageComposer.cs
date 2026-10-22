using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Ingame;

/// <summary>Game2FullGameStatusMessageComposer — the whole game as it stands, for a client that lost step</summary>
[Message(ServerMessages.Game2FullGameStatusMessageComposer)]
public sealed partial record Game2FullGameStatusMessageComposer(Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data.FullGameStatusData Status);
