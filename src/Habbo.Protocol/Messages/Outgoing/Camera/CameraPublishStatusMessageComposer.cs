using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Camera;

/// <summary>CameraPublishStatusMessageComposer — whether the photo was published; when not, how long until the next
/// may be; when it was, the published photo's id.</summary>
[Message(ServerMessages.CameraPublishStatusMessageComposer)]
public sealed partial record CameraPublishStatusMessageComposer(bool Ok, int SecondsToWait, string? ExtraDataId);
