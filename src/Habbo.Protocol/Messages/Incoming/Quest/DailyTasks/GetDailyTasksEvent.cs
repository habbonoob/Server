using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Quest.DailyTasks;

/// <summary>GetDailyTasks — the player's daily tasks, asked for at login and when the day's run out.</summary>
[Incoming(ClientMessages.GetDailyTasksEvent)]
public sealed partial record GetDailyTasksEvent;
