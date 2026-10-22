using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Camera;

/// <summary>CameraStorageUrlMessageComposer — where the rendered photo is, relative to stories.image_url_base. Empty
/// when it could not be rendered.</summary>
[Message(ServerMessages.CameraStorageUrlMessageComposer)]
public sealed partial record CameraStorageUrlMessageComposer(string Url);
