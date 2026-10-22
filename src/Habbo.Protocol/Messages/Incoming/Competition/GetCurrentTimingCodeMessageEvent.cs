using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Competition;

/// <summary>1137 GetCurrentTimingCode — a landing-view widget asks what is scheduled for the
/// name it reads out of its own configuration.</summary>
[Incoming(ClientMessages.GetCurrentTimingCodeMessageEvent)]
public sealed partial record GetCurrentTimingCodeMessageEvent(string SchedulingKey);
