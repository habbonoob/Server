using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.GroupForums;

/// <summary>PostMessageMessageComposer</summary>
[Message(ServerMessages.PostMessageMessageComposer)]
public sealed partial record PostMessageMessageComposer(int GroupId, int ThreadId, ForumMessageData Message);
