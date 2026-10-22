using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Engine;

/// <summary>SetObjectDataMessageEvent — an item's map data, as keys and values in turn</summary>
[Incoming(ClientMessages.SetObjectDataMessageEvent)]
public sealed partial record SetObjectDataMessageEvent(int ItemId, IReadOnlyList<string> KeysAndValues);
