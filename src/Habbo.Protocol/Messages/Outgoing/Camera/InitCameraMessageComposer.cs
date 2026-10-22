using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Camera;

/// <summary>InitCameraMessageComposer — what a photo costs to buy (credits and duckets) and to publish (duckets).</summary>
[Message(ServerMessages.InitCameraMessageComposer)]
public sealed partial record InitCameraMessageComposer(int CreditPrice, int DucketPrice, int PublishDucketPrice);
