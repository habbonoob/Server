using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>507 ExtendedProfileChangedMessageComposer — a profile the client may be showing changed.</summary>
[Message(ServerMessages.ExtendedProfileChangedMessageComposer)]
public sealed partial record ExtendedProfileChangedMessageComposer(int UserId);
