namespace Habbo.Gateway;

/// <summary>Hotel-wide switches the client reads at login (config section "Hotel").</summary>
public sealed class HotelOptions
{
    /// <summary>The client's "authentic Habbo" availability flag.</summary>
    public bool IsAuthenticHabbo { get; set; } = true;

    /// <summary>Send players straight into their home room after login.</summary>
    public bool EnterHomeRoomOnLogin { get; set; }

    /// <summary>The colour of the mystery box the hotel is currently running an event with,
    /// and of the key that opens it. Both empty when no event is on, which is what the client
    /// reads as "do not show the mystery box".</summary>
    public string MysteryBoxColor { get; set; } = string.Empty;

    public string MysteryBoxKeyColor { get; set; } = string.Empty;

    /// <summary>Which achievement category the dialog opens on. A name with no achievements
    /// under it is ignored in favour of one that has some, so this cannot leave the client with
    /// nothing selected.</summary>
    public string AchievementsDefaultCategory { get; set; } = string.Empty;

    /// <summary>Friends per friend-list fragment; long lists arrive in several.</summary>
    public int FriendListFragmentSize { get; set; } = 300;

    /// <summary>Items per FurniList fragment; large inventories arrive in several.</summary>
    public int InventoryFragmentSize { get; set; } = 1000;
}
