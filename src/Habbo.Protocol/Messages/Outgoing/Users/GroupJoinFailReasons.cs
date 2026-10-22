namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>HabboGroupJoinFailed's reasons, as group.joinfail.{n} words them.</summary>
public static class GroupJoinFailReasons
{
    public const int GroupFull = 0;
    public const int TooManyGroups = 1;
    public const int GroupClosed = 2;
    public const int NoRequestsAccepted = 3;

    /// <summary>The client opens its club offer instead of an alert.</summary>
    public const int ClubRequired = 4;

    public const int ApplicantNotClub = 5;
    public const int ApplicantAtLimit = 6;
}
