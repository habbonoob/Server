using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.GroupForums;

/// <summary>UpdateMessageMessageComposer</summary>
[Message(ServerMessages.UpdateMessageMessageComposer)]
public sealed partial record UpdateMessageMessageComposer(int GroupId, int ThreadId, ForumMessageData Message);
