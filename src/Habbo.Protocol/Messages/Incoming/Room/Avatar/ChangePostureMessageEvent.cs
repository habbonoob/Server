using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Avatar;

/// <summary>2051 ChangePosture — sit or stand.</summary>
[Incoming(ClientMessages.ChangePostureMessageEvent)]
public sealed partial record ChangePostureMessageEvent(int Posture);
