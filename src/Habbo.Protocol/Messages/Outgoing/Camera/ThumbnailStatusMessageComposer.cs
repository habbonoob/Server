using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Camera;

/// <summary>ThumbnailStatusMessageComposer — whether the room's thumbnail was rendered, and whether it was refused for
/// rendering too many.</summary>
[Message(ServerMessages.ThumbnailStatusMessageComposer)]
public sealed partial record ThumbnailStatusMessageComposer(bool Ok, bool RenderLimitHit);
