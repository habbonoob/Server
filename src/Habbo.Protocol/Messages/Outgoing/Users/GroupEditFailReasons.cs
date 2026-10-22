namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>GuildEditFailed's reasons, as group.edit.fail.{n} words them.</summary>
public static class GroupEditFailReasons
{
    public const int RoomAlreadyHome = 0;
    public const int InvalidName = 1;

    /// <summary>The client opens its club offer instead of an alert.</summary>
    public const int ClubRequired = 2;

    public const int TooManyGroups = 3;
    public const int SafetyLocked = 4;
}
