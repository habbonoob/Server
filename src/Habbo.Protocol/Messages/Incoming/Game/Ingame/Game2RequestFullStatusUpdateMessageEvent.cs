using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Game.Ingame;

/// <summary>Game2RequestFullStatusUpdateMessageEvent — the client lost step with the game and wants all of it again</summary>
[Incoming(ClientMessages.Game2RequestFullStatusUpdateMessageEvent)]
public sealed partial record Game2RequestFullStatusUpdateMessageEvent(int Reason);
