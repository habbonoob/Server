using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Handshake;

/// <summary>DisconnectReasonComposer — why the server is closing the connection</summary>
[Message(ServerMessages.DisconnectReasonComposer)]
public sealed partial record DisconnectReasonComposer(int Reason)
{
    /// <summary>The reasons the client names (DisconnectReasonEvent).</summary>
    public const int MaintenanceBreak = -2;
    public const int Logout = 0;
    public const int JustBanned = 1;
    public const int ConcurrentLogin = 2;
    public const int HotelClosed = 12;
    public const int PongTimeout = 113;
}
