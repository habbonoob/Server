using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Availability;

/// <summary>MaintenanceStatusMessageComposer — maintenance is coming or under way, for so many minutes</summary>
[Message(ServerMessages.MaintenanceStatusMessageComposer)]
public sealed partial record MaintenanceStatusMessageComposer(bool IsInMaintenance, int MinutesUntilMaintenance, int Duration);
