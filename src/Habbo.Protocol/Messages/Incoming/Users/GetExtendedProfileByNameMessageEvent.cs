using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>1912 GetExtendedProfileByName.</summary>
[Incoming(ClientMessages.GetExtendedProfileByNameMessageEvent)]
public sealed partial record GetExtendedProfileByNameMessageEvent(string Name);
