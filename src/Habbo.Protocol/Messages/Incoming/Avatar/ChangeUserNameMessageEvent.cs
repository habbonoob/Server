using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Avatar;

/// <summary>3580 ChangeUserName — take this name.</summary>
[Incoming(ClientMessages.ChangeUserNameMessageEvent)]
public sealed partial record ChangeUserNameMessageEvent(string Name);
