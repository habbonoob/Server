using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Furniture;

/// <summary>AreaHideMessageComposer</summary>
[Message(ServerMessages.AreaHideMessageComposer)]
public sealed partial record AreaHideMessageComposer(AreaHideWireData Area);
