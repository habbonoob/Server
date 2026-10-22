using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Availability;

/// <summary>764 AvailabilityStatusMessageComposer.</summary>
[Message(ServerMessages.AvailabilityStatusMessageComposer)]
public sealed partial record AvailabilityStatusMessageComposer(
    bool IsOpen, bool OnShutdown, bool IsAuthenticHabbo);
