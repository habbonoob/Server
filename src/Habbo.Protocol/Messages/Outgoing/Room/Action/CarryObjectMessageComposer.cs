using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Action;

/// <summary>2684 CarryObjectMessageComposer — what a user carries in their hand; 0 is nothing.</summary>
[Message(ServerMessages.CarryObjectMessageComposer)]
public sealed partial record CarryObjectMessageComposer(int UserId, int ItemType);
