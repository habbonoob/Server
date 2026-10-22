using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>3554 BlockUserUpdateMessageComposer.</summary>
[Message(ServerMessages.BlockUserUpdateMessageComposer)]
public sealed partial record BlockUserUpdateMessageComposer(int Result, int UserId);
