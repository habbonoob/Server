using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Players;

/// <summary>A player's account preferences as AccountPreferencesComposer (2809) carries them.</summary>
[GenerateSerializer]
public partial record AccountPreferences(
    [property: Id(0)] int UiVolume,
    [property: Id(1)] int FurniVolume,
    [property: Id(2)] int TraxVolume,
    [property: Id(3)] bool FreeFlowChatDisabled,
    [property: Id(4)] bool RoomInvitesIgnored,
    [property: Id(5)] bool RoomCameraFollowDisabled,
    [property: Id(6)] int UiFlags,
    [property: Id(7)] int PreferedChatStyle,
    [property: Id(8)] bool WiredMenuButton,
    [property: Id(9)] bool WiredInspectButton,
    [property: Id(10)] bool PlayTestMode,
    [property: Id(11)] bool WiredWhisperDisabled,
    [property: Id(12)] bool ShowAllNotifications,
    [property: Id(13)] string WiredUiStyle,
    [property: Id(14)] bool InfoFeedEnabled,
    [property: Id(15)] int ChatFontSize,
    [property: Id(16)] int ChatMode,
    [property: Id(17)] int ChatBubbleWidth,
    [property: Id(18)] int ChatScrollSpeed,
    [property: Id(19)] OnlineIndicatorPreference OnlineIndicator);
