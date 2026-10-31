using Habbo.Domain.Users;

namespace Habbo.Domain.DailyTasks;

/// <summary>A daily task set for a player: the day it is for (UTC), which task of the pool (its code), how many times
/// they have done it, and whether it is done and its reward claimed. Its id is the task's id on the wire.</summary>
public sealed class DailyTask
{
    public const int MaxCodeLength = 64;

    public const byte InProgress = 0;
    public const byte Completed = 1;
    public const byte Claimed = 2;

    public int Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public DateOnly Day { get; set; }
    public string Code { get; set; } = string.Empty;
    public int Repeats { get; set; }
    public byte Status { get; set; }
}
