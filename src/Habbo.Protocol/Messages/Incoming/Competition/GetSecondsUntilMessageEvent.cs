using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Competition;

/// <summary>GetSecondsUntilMessageEvent — how long until a time of the hotel's ("2026-04-15 15:00"), for a landing view countdown</summary>
[Incoming(ClientMessages.GetSecondsUntilMessageEvent)]
public sealed partial record GetSecondsUntilMessageEvent(string TimeStr);
