using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>GuildMembershipsMessageComposer</summary>
[Message(ServerMessages.GuildMembershipsMessageComposer)]
public sealed partial record GuildMembershipsMessageComposer(IReadOnlyList<Habbo.Protocol.Messages.Outgoing.Users.HabboGroupEntryData> Guilds);
