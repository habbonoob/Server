using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Players;

/// <summary>A partial preferences change; null fields stay as they are.</summary>
[GenerateSerializer]
public partial record PreferencesUpdate(
    [property: Id(0)] int? UiVolume = null,
    [property: Id(1)] int? FurniVolume = null,
    [property: Id(2)] int? TraxVolume = null,
    [property: Id(3)] bool? FreeFlowChatDisabled = null,
    [property: Id(4)] bool? RoomInvitesIgnored = null,
    [property: Id(5)] bool? RoomCameraFollowDisabled = null,
    [property: Id(6)] int? PreferedChatStyle = null,
    [property: Id(7)] bool? WiredMenuButton = null,
    [property: Id(8)] bool? WiredInspectButton = null,
    [property: Id(9)] bool? PlayTestMode = null,
    [property: Id(10)] bool? WiredWhisperDisabled = null,
    [property: Id(11)] bool? ShowAllNotifications = null,
    [property: Id(12)] string? WiredUiStyle = null,
    [property: Id(13)] int? UiFlags = null,
    [property: Id(14)] DateTimeOffset? CatalogNewAdditionsSeenAt = null,
    [property: Id(15)] int? ChatFontSize = null,
    [property: Id(16)] int? ChatMode = null,
    [property: Id(17)] int? ChatBubbleWidth = null,
    [property: Id(18)] int? ChatScrollSpeed = null,
    [property: Id(19)] OnlineIndicatorPreference? OnlineIndicator = null);
