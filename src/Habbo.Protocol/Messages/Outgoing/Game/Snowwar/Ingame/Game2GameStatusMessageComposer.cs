using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Ingame;

/// <summary>Game2GameStatusMessageComposer — one turn: its checksum and the events of the next</summary>
[Message(ServerMessages.Game2GameStatusMessageComposer)]
public sealed partial record Game2GameStatusMessageComposer(Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data.GameStatusData Status);
