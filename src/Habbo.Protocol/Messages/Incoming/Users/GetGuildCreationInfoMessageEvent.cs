using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>GetGuildCreationInfoMessageEvent — the price and the rooms a group could be made for</summary>
[Incoming(ClientMessages.GetGuildCreationInfoMessageEvent)]
public sealed partial record GetGuildCreationInfoMessageEvent;
