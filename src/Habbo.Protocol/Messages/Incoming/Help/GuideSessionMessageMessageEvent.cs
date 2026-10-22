using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>GuideSessionMessageMessageEvent — say something in the session</summary>
[Incoming(ClientMessages.GuideSessionMessageMessageEvent)]
public sealed partial record GuideSessionMessageMessageEvent(string Message);
