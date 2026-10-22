using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.VariableFx;

/// <summary>Variable displays gone from the room, by id.</summary>
[Message(ServerMessages.VariableFxConfigsRemovedMessageComposer)]
public sealed partial record VariableFxConfigsRemovedMessageComposer(IReadOnlyList<int> ConfigIds);
