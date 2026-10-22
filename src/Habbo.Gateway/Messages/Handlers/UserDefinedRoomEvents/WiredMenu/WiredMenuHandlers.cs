using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredMenu;
using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredMenu.RoomLogs;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Wired;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredMenu;

/// <summary>1817 WiredGetRoomSettings.</summary>
internal sealed class WiredGetRoomSettingsHandler : IMessageHandler<WiredGetRoomSettingsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredGetRoomSettingsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.GetWiredSettingsAsync(player.Id);
        return true;
    }
}

/// <summary>1325 WiredSetRoomSettings.</summary>
internal sealed class WiredSetRoomSettingsHandler : IMessageHandler<WiredSetRoomSettingsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredSetRoomSettingsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SetWiredSettingsAsync(player.Id, message.ModifyPermissionMask, message.ReadPermissionMask, message.Timezone);
        return true;
    }
}

/// <summary>1692 WiredGetRoomLogs.</summary>
internal sealed class WiredGetRoomLogsHandler : IMessageHandler<WiredGetRoomLogsEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredGetRoomLogsEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.GetWiredLogsAsync(player.Id,
            new WiredLogQuery(message.Page, message.PageSize, message.LevelFilter, message.SourceFilter, message.Query));
        return true;
    }
}

/// <summary>2934 WiredGetRoomStats.</summary>
internal sealed class WiredGetRoomStatsHandler : IMessageHandler<WiredGetRoomStatsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredGetRoomStatsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.GetWiredStatsAsync(player.Id);
        return true;
    }
}

/// <summary>1540 WiredGetErrorLogs.</summary>
internal sealed class WiredGetErrorLogsHandler : IMessageHandler<WiredGetErrorLogsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredGetErrorLogsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.GetWiredErrorsAsync(player.Id);
        return true;
    }
}

/// <summary>3608 WiredClearErrorLogs.</summary>
internal sealed class WiredClearErrorLogsHandler : IMessageHandler<WiredClearErrorLogsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredClearErrorLogsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.ClearWiredErrorsAsync(player.Id);
        return true;
    }
}

/// <summary>3825 WiredSetPreferences.</summary>
internal sealed class WiredSetPreferencesHandler : IMessageHandler<WiredSetPreferencesMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredSetPreferencesMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.UpdatePreferencesAsync(new PreferencesUpdate(
            WiredMenuButton: message.WiredMenuButton,
            WiredInspectButton: message.WiredInspectButton,
            PlayTestMode: message.PlayTestMode,
            WiredWhisperDisabled: message.WiredWhisperDisabled,
            ShowAllNotifications: message.ShowAllNotifications,
            WiredUiStyle: message.UiStyle));
        return true;
    }
}

/// <summary>3848 WiredGetAllVariablesHash.</summary>
internal sealed class WiredGetAllVariablesHashHandler : IMessageHandler<WiredGetAllVariablesHashMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredGetAllVariablesHashMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.GetWiredVariablesHashAsync(player.Id);
        return true;
    }
}

/// <summary>671 WiredGetAllVariablesDiffs.</summary>
internal sealed class WiredGetAllVariablesDiffsHandler : IMessageHandler<WiredGetAllVariablesDiffsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredGetAllVariablesDiffsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.GetWiredVariablesDiffsAsync(player.Id, message.Known.Select(k => new KnownWiredVariable(k.Id, k.Hash)).ToList());
        return true;
    }
}
