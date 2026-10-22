using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.AvatarEffect;

[WireStruct]
public sealed partial record AvatarEffectData(
    int Type, int SubType, int Duration, int InactiveEffectsInInventory, int SecondsLeftIfActive, bool IsPermanent)
{
    /// <summary>What <see cref="SecondsLeftIfActive"/> says of an effect nobody has started.</summary>
    public const int NotActive = -1;
}
