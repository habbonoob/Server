namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>ChangeEmailResult's answers, as welcome.gift.email.error.{n} words them.</summary>
public static class EmailChangeResults
{
    public const int Changed = 0;
    public const int Invalid = 1;
    public const int TooLong = 2;
    public const int InUse = 3;
    public const int TooManyChanges = 4;
}
