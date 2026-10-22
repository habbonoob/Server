using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>3428 PerkAllowances — ask for the perks again.</summary>
[Incoming(ClientMessages.PerkAllowancesMessageEvent)]
public sealed partial record PerkAllowancesMessageEvent;
