using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Avatar;

/// <summary>196 CheckUserName — would this name do?</summary>
[Incoming(ClientMessages.CheckUserNameMessageEvent)]
public sealed partial record CheckUserNameMessageEvent(string Name);
