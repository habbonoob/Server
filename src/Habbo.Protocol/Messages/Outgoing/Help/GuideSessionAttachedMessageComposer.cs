using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>GuideSessionAttachedMessageComposer — a request is waiting for a guide, or is offered to one</summary>
[Message(ServerMessages.GuideSessionAttachedMessageComposer)]
public sealed partial record GuideSessionAttachedMessageComposer(bool AsGuide, int HelpRequestType, string HelpRequestDescription, int RoleSpecificWaitTime);
