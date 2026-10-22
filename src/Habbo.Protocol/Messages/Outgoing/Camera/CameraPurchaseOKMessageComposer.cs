using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Camera;

/// <summary>CameraPurchaseOKMessageComposer — the photo was bought and is in the inventory.</summary>
[Message(ServerMessages.CameraPurchaseOKMessageComposer)]
public sealed partial record CameraPurchaseOKMessageComposer;
