using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.VariableFx;

/// <summary>How a variable display looks and when it shows: its id (the add-on's item id), whether it shows over users
/// or furni, when (always, on a change of the kinds in the mask, or on hover), for how long, its category and style, its
/// colour, width and renderer, the range it shows by default, and its extras.</summary>
[WireStruct]
public sealed partial record VariableFxConfigData(
    int ConfigId, bool IsUserFx, int ShowMode, int UpdateMask, bool ShowOnMouseHover, int ShowDuration,
    int CategoryId, int StyleId, int ColorId, int WidthId, int RendererId, long DefaultMin, long DefaultMax,
    IReadOnlyList<VariableFxExtraData> Extras);
