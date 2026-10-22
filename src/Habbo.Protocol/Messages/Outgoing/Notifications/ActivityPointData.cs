using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Notifications;

[WireStruct]
public sealed partial record ActivityPointData(int Type, int Amount);
