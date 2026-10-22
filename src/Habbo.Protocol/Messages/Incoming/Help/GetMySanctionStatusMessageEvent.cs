using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>GetMySanctionStatusMessageEvent</summary>
[Incoming(ClientMessages.GetMySanctionStatusMessageEvent)]
public sealed partial record GetMySanctionStatusMessageEvent;
