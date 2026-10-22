namespace Habbo.Silo.Abstractions.Rooms;

/// <summary>What the room settings window saves. A setting the client's release does not send is null, and stays as
/// it is.</summary>
[GenerateSerializer]
public sealed partial record RoomSettingsUpdate(
    [property: Id(0)] string Name,
    [property: Id(1)] string Description,
    [property: Id(2)] int DoorMode,
    [property: Id(3)] string Password,
    [property: Id(4)] int MaximumVisitors,
    [property: Id(5)] int CategoryId,
    [property: Id(6)] IReadOnlyList<string> Tags,
    [property: Id(7)] int TradeMode,
    [property: Id(8)] bool AllowPets,
    [property: Id(9)] bool AllowFoodConsume,
    [property: Id(10)] bool AllowWalkThrough,
    [property: Id(11)] bool HideWalls,
    [property: Id(12)] int WallThickness,
    [property: Id(13)] int FloorThickness,
    [property: Id(14)] int WhoCanMute,
    [property: Id(15)] int WhoCanKick,
    [property: Id(16)] int WhoCanBan,
    [property: Id(17)] int? ChatMode,
    [property: Id(18)] int? ChatBubbleSize,
    [property: Id(19)] int? ChatScrollSpeed,
    [property: Id(20)] int? ChatHearRange,
    [property: Id(21)] int ChatFloodSensitivity,
    [property: Id(22)] bool? AllowNavigatorDynamicCategories,
    [property: Id(23)] bool? LeaveOnDoorTile = null,
    [property: Id(24)] bool? IdleSleepEnabled = null,
    [property: Id(25)] int? IdleSleepTimeoutSeconds = null,
    [property: Id(26)] bool? IdleAutokickEnabled = null,
    [property: Id(27)] int? IdleAutokickTimeoutSeconds = null,
    [property: Id(28)] bool? MuteAllPets = null);
