namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>GuildMemberMgmtFailed's reasons, as group.membermgmt.fail.{n} words them.</summary>
public static class GroupMemberManagementFailReasons
{
    public const int NotMember = 0;
    public const int AlreadyRejected = 1;
    public const int AlreadyAccepted = 2;
}
