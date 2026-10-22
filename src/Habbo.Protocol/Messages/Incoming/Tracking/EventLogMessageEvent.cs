using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Tracking;

/// <summary>3803 EventLog — client-side telemetry we accept and ignore.</summary>
[Incoming(ClientMessages.EventLogMessageEvent)]
public sealed partial record EventLogMessageEvent(string Category, string Action, string Label);
