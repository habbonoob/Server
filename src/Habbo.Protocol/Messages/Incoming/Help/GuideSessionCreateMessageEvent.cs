using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>GuideSessionCreateMessageEvent — ask for a guide: a tour, help, or a bully report</summary>
[Incoming(ClientMessages.GuideSessionCreateMessageEvent)]
public sealed partial record GuideSessionCreateMessageEvent(int RequestType, string Description);
