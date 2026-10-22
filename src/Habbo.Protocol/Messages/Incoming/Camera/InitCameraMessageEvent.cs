using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Camera;

/// <summary>InitCameraMessageEvent — the camera's prices, asked for when the room view opens.</summary>
[Incoming(ClientMessages.InitCameraMessageEvent)]
public sealed partial record InitCameraMessageEvent;
