namespace Habbo.Protocol.Messages.Outgoing.Avatar;

/// <summary>The answers CheckUserNameResult and ChangeUserNameResult carry, as the client's name
/// change view reads them.</summary>
public static class NameResultCodes
{
    public const int Available = 0;
    public const int Unchecked = 1;
    public const int TooShort = 2;
    public const int TooLong = 3;
    public const int Invalid = 4;
    public const int Taken = 5;
    public const int ChangeNotAllowed = 6;
    public const int MergeHotelDown = 7;
}
