using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Discord;

/// <summary>GetDiscordPreferencesMessageEvent — how the player shows on Discord</summary>
[Incoming(ClientMessages.GetDiscordPreferencesMessageEvent)]
public sealed partial record GetDiscordPreferencesMessageEvent;
