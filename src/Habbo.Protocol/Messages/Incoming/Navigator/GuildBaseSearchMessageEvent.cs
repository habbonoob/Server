using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>GuildBaseSearchMessageEvent</summary>
[Incoming(ClientMessages.GuildBaseSearchMessageEvent)]
public sealed partial record GuildBaseSearchMessageEvent(int AdIndex);
