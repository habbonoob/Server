namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>What MemberData's type says a user is to the group.</summary>
public static class GroupMemberTypes
{
    public const int Owner = 0;
    public const int Admin = 1;
    public const int Member = 2;
    public const int Pending = 3;
    public const int Blocked = 4;
}
