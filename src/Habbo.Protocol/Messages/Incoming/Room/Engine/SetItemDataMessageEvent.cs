using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Engine;

/// <summary>SetItemDataMessageEvent — a note rewritten</summary>
[Incoming(ClientMessages.SetItemDataMessageEvent)]
public sealed partial record SetItemDataMessageEvent(int ItemId, string ColorHex, string Text);
