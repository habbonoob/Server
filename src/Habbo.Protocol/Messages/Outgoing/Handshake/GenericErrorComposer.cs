using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Handshake;

/// <summary>2929 GenericErrorComposer.</summary>
[Message(ServerMessages.GenericErrorComposer)]
public sealed partial record GenericErrorComposer(int ErrorCode);

/// <summary>The generic error codes the client acts on.</summary>
public static class GenericErrorCodes
{
    /// <summary>4008: the user was kicked out of the room.</summary>
    public const int Kicked = 4008;

    /// <summary>-100002: the room's password was wrong; the client asks again.</summary>
    public const int WrongPassword = -100002;
}
