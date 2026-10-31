using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Quest.DailyTasks;

/// <summary>DailyTasksActiveList — the player's tasks for today, and done ones of earlier days still unclaimed.</summary>
[Message(ServerMessages.DailyTasksActiveListMessageComposer)]
public sealed partial record DailyTasksActiveListMessageComposer(
    IReadOnlyList<DailyTaskData> Tasks);
