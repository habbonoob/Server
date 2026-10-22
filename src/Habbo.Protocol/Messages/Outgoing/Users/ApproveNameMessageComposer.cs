using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>278 ApproveNameMessageComposer — whether a name would be accepted, and if not why, with
/// what the client fills into its message.</summary>
[Message(ServerMessages.ApproveNameMessageComposer)]
public sealed partial record ApproveNameMessageComposer(int Result, string NameValidationInfo)
{
    public const int Accepted = 0;

    /// <summary>Too long: the info is the longest allowed.</summary>
    public const int TooLong = 1;

    /// <summary>Too short: the info is the shortest allowed.</summary>
    public const int TooShort = 2;

    /// <summary>Characters that are not allowed: the info lists them.</summary>
    public const int ForbiddenCharacters = 3;

    /// <summary>A filtered word: the info is where it starts.</summary>
    public const int ForbiddenWords = 4;
}
