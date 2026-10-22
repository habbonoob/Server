using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>ObjectsDataUpdateMessageComposer</summary>
[Message(ServerMessages.ObjectsDataUpdateMessageComposer)]
public sealed partial record ObjectsDataUpdateMessageComposer(IReadOnlyList<ObjectDataEntry> Objects);
