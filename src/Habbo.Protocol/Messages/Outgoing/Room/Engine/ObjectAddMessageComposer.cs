using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>3228 ObjectAddMessageComposer — one floor item, with its owner name.</summary>
[Message(ServerMessages.ObjectAddMessageComposer)]
public sealed partial record ObjectAddMessageComposer(FloorItemData Item);
