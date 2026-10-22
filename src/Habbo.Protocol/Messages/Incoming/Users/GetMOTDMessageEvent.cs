using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>2621 GetMOTD — the hotel's messages of the day.</summary>
[Incoming(ClientMessages.GetMOTDMessageEvent)]
public sealed partial record GetMOTDMessageEvent;
