using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>GroupDetailsChangedMessageComposer</summary>
[Message(ServerMessages.GroupDetailsChangedMessageComposer)]
public sealed partial record GroupDetailsChangedMessageComposer(int GroupId);
