namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>The member list's filter, in the order its drop menu lists them.</summary>
public static class GroupMemberSearchTypes
{
    public const int All = 0;
    public const int Admins = 1;
    public const int Pending = 2;
    public const int Blocked = 3;
}
