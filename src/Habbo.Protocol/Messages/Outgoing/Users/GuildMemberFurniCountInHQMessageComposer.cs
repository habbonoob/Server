using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>GuildMemberFurniCountInHQMessageComposer</summary>
[Message(ServerMessages.GuildMemberFurniCountInHQMessageComposer)]
public sealed partial record GuildMemberFurniCountInHQMessageComposer(int UserId, int FurniCount);
