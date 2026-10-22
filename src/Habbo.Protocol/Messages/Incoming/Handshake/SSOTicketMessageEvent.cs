using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Handshake;

/// <summary>2354 SSOTicket — the one-time login ticket, then the client's local time.</summary>
[Incoming(ClientMessages.SSOTicketMessageEvent)]
public sealed partial record SSOTicketMessageEvent(string Ticket, int? LocalTime);
