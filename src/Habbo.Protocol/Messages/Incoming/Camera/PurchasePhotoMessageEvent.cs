using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Camera;

/// <summary>PurchasePhotoMessageEvent — buy the photo last rendered, as a furni.</summary>
[Incoming(ClientMessages.PurchasePhotoMessageEvent)]
public sealed partial record PurchasePhotoMessageEvent;
