using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;

/// <summary>One furni or user highlighted by wired (set highlight), as the visualization states carry it (the client's
/// FurniHighlightData and UserHighlightData). Set highlight is not built: the states always send none.</summary>
[WireStruct]
public sealed partial record WiredHighlightData(int ObjectId, int HighlightType, int Strength, int Color, int AnimationMode, int EasingMode);
