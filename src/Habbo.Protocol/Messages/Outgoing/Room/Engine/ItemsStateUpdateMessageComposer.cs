using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>ItemsStateUpdateMessageComposer</summary>
[Message(ServerMessages.ItemsStateUpdateMessageComposer)]
public sealed partial record ItemsStateUpdateMessageComposer(IReadOnlyList<ItemStateData> Items);
