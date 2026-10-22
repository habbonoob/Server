using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Avatar;

/// <summary>2738 AvatarExpression — wave, blow a kiss, laugh, cry, jump, the respect thumbs-up, or
/// ":idle", which falls asleep (the client's AvatarExpressionEnum).</summary>
[Incoming(ClientMessages.AvatarExpressionMessageEvent)]
public sealed partial record AvatarExpressionMessageEvent(int Expression);
