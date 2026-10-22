using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>2883 WiredMovementsMessageComposer — wired moving things, which the client animates from
/// where they were to where they go over the given milliseconds: users walking or sliding, floor
/// furni, and users turning.</summary>
[Message(ServerMessages.WiredMovementsMessageComposer)]
public sealed partial record WiredMovementsMessageComposer(IReadOnlyList<WiredMovementData> Movements);
