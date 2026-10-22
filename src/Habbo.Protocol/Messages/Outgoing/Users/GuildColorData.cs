using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>GuildColorData — a colour as six hex digits</summary>
[WireStruct]
public sealed partial record GuildColorData(int Id, string Color);
