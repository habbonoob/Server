using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>ItemStateUpdateMessageComposer</summary>
[Message(ServerMessages.ItemStateUpdateMessageComposer)]
public sealed partial record ItemStateUpdateMessageComposer(int ItemId, string State);
