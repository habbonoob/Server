using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>GuideSessionStartedMessageComposer — a guide took the request</summary>
[Message(ServerMessages.GuideSessionStartedMessageComposer)]
public sealed partial record GuideSessionStartedMessageComposer(int RequesterUserId, string RequesterName, string RequesterFigure, int GuideUserId, string GuideName, string GuideFigure);
