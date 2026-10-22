using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>GuildCreatedMessageComposer</summary>
[Message(ServerMessages.GuildCreatedMessageComposer)]
public sealed partial record GuildCreatedMessageComposer(int BaseRoomId, int GroupId);
