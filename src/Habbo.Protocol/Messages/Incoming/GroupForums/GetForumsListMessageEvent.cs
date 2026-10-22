using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.GroupForums;

/// <summary>GetForumsListMessageEvent</summary>
[Incoming(ClientMessages.GetForumsListMessageEvent)]
public sealed partial record GetForumsListMessageEvent(int ListCode, int StartIndex, int Amount);
