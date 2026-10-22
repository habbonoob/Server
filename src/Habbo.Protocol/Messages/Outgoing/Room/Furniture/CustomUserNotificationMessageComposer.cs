using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Furniture;

/// <summary>1836 CustomUserNotificationMessageComposer — the user lacks what a furni asks of them; the client opens
/// the notice for the code.</summary>
[Message(ServerMessages.CustomUserNotificationMessageComposer)]
public sealed partial record CustomUserNotificationMessageComposer(int Code)
{
    /// <summary>A costume hopper, entered out of costume.</summary>
    public const int CostumeHopper = 1;

    /// <summary>A hopper for club members only.</summary>
    public const int VipHopper = 2;

    /// <summary>A gate for club members only.</summary>
    public const int VipGate = 3;
}
