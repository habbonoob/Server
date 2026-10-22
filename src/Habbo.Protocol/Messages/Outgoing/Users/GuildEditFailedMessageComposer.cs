using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>GuildEditFailedMessageComposer</summary>
[Message(ServerMessages.GuildEditFailedMessageComposer)]
public sealed partial record GuildEditFailedMessageComposer(int Reason);
