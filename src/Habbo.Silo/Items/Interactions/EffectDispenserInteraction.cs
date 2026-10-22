namespace Habbo.Silo.Items.Interactions;

/// <summary>A dispenser of what is worn rather than carried: a weapon rack's musket or sword, a lei. The avatar wears
/// it in this room only, as an effect, until it puts on another or leaves.</summary>
[FurniInteraction("effect_vending_machine")]
public sealed class EffectDispenserInteraction : DispenserInteraction
{
    protected override void Give(FurniUse use, int id) => use.Room.WearEffect(use.User, id);
}
