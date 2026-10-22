namespace Habbo.Silo.Items.Interactions;

/// <summary>A stack helper: whatever is put on its tiles stands at the height it is set to, rather
/// than on top of what is there. The height is kept in furni_stack_heights; the room applies it.</summary>
[FurniInteraction("furniture_custom_stack_height")]
public sealed class StackHelperInteraction : FurniInteraction;
