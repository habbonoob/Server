using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>AppealCfhMessageEvent</summary>
[Incoming(ClientMessages.AppealCfhMessageEvent)]
public sealed partial record AppealCfhMessageEvent(int CallId);
