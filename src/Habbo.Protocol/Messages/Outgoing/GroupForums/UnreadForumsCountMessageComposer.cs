using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.GroupForums;

/// <summary>UnreadForumsCountMessageComposer</summary>
[Message(ServerMessages.UnreadForumsCountMessageComposer)]
public sealed partial record UnreadForumsCountMessageComposer(int UnreadForumsCount);
