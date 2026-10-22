using Habbo.Domain.Users;
using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Protocol.Messages.Outgoing.Handshake;

/// <summary>1203 UserRightsMessageComposer.</summary>
[Message(ServerMessages.UserRightsMessageComposer)]
public sealed partial record UserRightsMessageComposer(int ClubLevel, int SecurityLevel, bool IsAmbassador)
{
    public static UserRightsMessageComposer From(PlayerData player) =>
        new((int)player.ClubLevel, (int)player.Rank, player.IsAmbassador);
}
