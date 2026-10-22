using Habbo.Domain.Users;
using Habbo.Protocol.Messages.Outgoing.Room.Furniture;

namespace Habbo.Silo.Items.Interactions;

/// <summary>A gate for club members: they walk through it, nobody else does, and anyone else who tries it is told it
/// is for club members.</summary>
[FurniInteraction("vip_gate")]
public sealed class VipGateInteraction : FurniInteraction
{
    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Everyone;

    public override Task UseAsync(FurniUse use)
    {
        if (use.User.Player.ClubLevel == ClubLevel.None)
            use.Room.Tell(use.User, new CustomUserNotificationMessageComposer(CustomUserNotificationMessageComposer.VipGate));
        return Task.CompletedTask;
    }
}
