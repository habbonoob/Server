using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Engine;

/// <summary>GetItemDataMessageEvent — a note's colour and text</summary>
[Incoming(ClientMessages.GetItemDataMessageEvent)]
public sealed partial record GetItemDataMessageEvent(int ItemId);
