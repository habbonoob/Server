using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Quest.DailyTasks;

/// <summary>ClaimDailyTask — the player claims a done daily task's reward.</summary>
[Incoming(ClientMessages.ClaimDailyTaskEvent)]
public sealed partial record ClaimDailyTaskEvent(
    int TaskId);
