using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Competition;

/// <summary>VoteForRoomMessageEvent — a vote for the room the player is in</summary>
[Incoming(ClientMessages.VoteForRoomMessageEvent)]
public sealed partial record VoteForRoomMessageEvent(string GoalCode);
