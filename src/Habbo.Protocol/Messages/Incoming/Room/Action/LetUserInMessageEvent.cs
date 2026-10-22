using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Action;

/// <summary>LetUserInMessageEvent</summary>
[Incoming(ClientMessages.LetUserInMessageEvent)]
public sealed partial record LetUserInMessageEvent(string UserName, bool Allow);
