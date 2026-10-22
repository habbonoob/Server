using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Camera;

/// <summary>RenderRoomThumbnailMessageEvent — the room's navigator thumbnail to render, as RenderRoom's data.</summary>
[Incoming(ClientMessages.RenderRoomThumbnailMessageEvent)]
public sealed partial record RenderRoomThumbnailMessageEvent(IReadOnlyList<byte> Data);
