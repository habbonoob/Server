using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>2595 UserNameChangedMessageComposer — a user in the room took a new name.</summary>
[Message(ServerMessages.UserNameChangedMessageComposer)]
public sealed partial record UserNameChangedMessageComposer(int WebId, int Id, string NewName);
