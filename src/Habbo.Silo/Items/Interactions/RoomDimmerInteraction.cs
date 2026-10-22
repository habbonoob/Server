namespace Habbo.Silo.Items.Interactions;

/// <summary>A room dimmer: tints the whole room from one of its saved presets. Its state is the
/// preset shown (<see cref="DimmerState"/>); its presets are kept in furni_dimmer_presets. The room
/// runs its dialog.</summary>
[FurniInteraction("furniture_roomdimmer")]
public sealed class RoomDimmerInteraction : FurniInteraction;
