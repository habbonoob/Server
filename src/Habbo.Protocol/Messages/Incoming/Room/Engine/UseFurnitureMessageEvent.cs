using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Engine;

/// <summary>2183 UseFurniture — a double-click on a floor item, or the infostand's Use button.
/// Param is 0 from a plain click; the server decides what the item's next state is.</summary>
[Incoming(ClientMessages.UseFurnitureMessageEvent)]
public sealed partial record UseFurnitureMessageEvent(int ItemId, int Param);
