using Habbo.Protocol.Messages.Outgoing.Room.Session;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>Configuration furniture: placed and switched on, each turns a room setting on
/// (Rooms:ConfigurationItems names which furni sets what).</summary>
public sealed partial class RoomGrain
{
    /// <summary>The state a configuration furni is off in.</summary>
    private const string ConfigurationOff = "0";

    private RoomConfigurationStates? _configuration;

    private RoomConfigurationStates ConfigurationStates()
    {
        var on = _items is null
            ? []
            : _items.Items
                .Where(item => item.Data.State != ConfigurationOff && item.Data.State != string.Empty)
                .Select(item => roomOptions.Value.ConfigurationItems.TryGetValue(item.Item.Definition!.ClassName, out var setting) ? setting : (ConfigurationSetting?)null)
                .OfType<ConfigurationSetting>()
                .ToHashSet();

        return new RoomConfigurationStates(
            on.Contains(ConfigurationSetting.HanditemControlBlocked),
            on.Contains(ConfigurationSetting.ChooserDisabled),
            on.Contains(ConfigurationSetting.FreeFurniMovements),
            on.Contains(ConfigurationSetting.InvisibleFurni));
    }

    /// <summary>Tells the room when a configuration furni changed what is switched on.</summary>
    private void RefreshConfiguration()
    {
        var states = ConfigurationStates();
        if (states == _configuration)
            return;

        _configuration = states;
        Broadcast(ConfigurationItemStatesMessageComposer.From(states));
    }

    /// <summary>Hand items stay where they are while a blocker is on, except for the room's own people.</summary>
    private bool HandItemsBlockedFor(RoomEntityStore.RoomEntity entity) =>
        (_configuration ?? ConfigurationStates()).HanditemControlBlocked && entity.ControllerLevel < RoomControllerLevels.Rights;
}
