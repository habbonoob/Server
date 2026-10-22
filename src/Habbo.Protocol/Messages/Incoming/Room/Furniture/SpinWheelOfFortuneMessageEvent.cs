using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>SpinWheelOfFortuneMessageEvent</summary>
[Incoming(ClientMessages.SpinWheelOfFortuneMessageEvent)]
public sealed partial record SpinWheelOfFortuneMessageEvent(int ItemId);
