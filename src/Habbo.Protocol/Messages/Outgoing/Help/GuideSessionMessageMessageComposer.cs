using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>GuideSessionMessageMessageComposer — something said in the session</summary>
[Message(ServerMessages.GuideSessionMessageMessageComposer)]
public sealed partial record GuideSessionMessageMessageComposer(string ChatMessage, int SenderId);
