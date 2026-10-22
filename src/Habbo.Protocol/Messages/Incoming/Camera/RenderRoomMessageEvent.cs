using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Camera;

/// <summary>RenderRoomMessageEvent — a photo to render: the room as the client sees it (planes, sprites, effects),
/// zlib-deflated JSON, written as a ByteArray (its length, then its bytes).</summary>
[Incoming(ClientMessages.RenderRoomMessageEvent)]
public sealed partial record RenderRoomMessageEvent(IReadOnlyList<byte> Data);
