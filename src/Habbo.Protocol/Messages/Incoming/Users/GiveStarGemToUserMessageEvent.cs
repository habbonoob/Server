using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>2701 GiveStarGemToUser.</summary>
[Incoming(ClientMessages.GiveStarGemToUserMessageEvent)]
public sealed partial record GiveStarGemToUserMessageEvent(int UserId, int Amount);
