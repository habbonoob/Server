using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Action;

/// <summary>3018 ExpressionMessageComposer — an avatar waves, laughs, and so on.</summary>
[Message(ServerMessages.ExpressionMessageComposer)]
public sealed partial record ExpressionMessageComposer(int RoomIndex, int Expression);
