using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Camera;

/// <summary>PublishPhotoMessageEvent — publish the photo last rendered to the player's stories.</summary>
[Incoming(ClientMessages.PublishPhotoMessageEvent)]
public sealed partial record PublishPhotoMessageEvent;
