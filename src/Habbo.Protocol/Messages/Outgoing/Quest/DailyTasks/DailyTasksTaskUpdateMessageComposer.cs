using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Quest.DailyTasks;

/// <summary>DailyTasksTaskUpdate — a daily task's new repeats and status; the client tells the player when one is done
/// and when its reward is claimed.</summary>
[Message(ServerMessages.DailyTasksTaskUpdateMessageComposer)]
public sealed partial record DailyTasksTaskUpdateMessageComposer(
    long TaskId,
    int Repeats,
    byte Status,
    int SecondsLeft);
