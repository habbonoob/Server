using Habbo.Protocol.Messages.Incoming.RoomSettings;
using Habbo.Silo.Abstractions.Rooms;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.RoomSettings;

/// <summary>SaveRoomSettingsMessageEvent — the room settings window's save.</summary>
internal sealed class SaveRoomSettingsHandler(IGrainFactory grains) : IMessageHandler<SaveRoomSettingsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SaveRoomSettingsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await grains.GetGrain<IGameRoomGrain>(message.RoomId).SaveSettingsAsync(player, new RoomSettingsUpdate(
            message.Name, message.Description, message.DoorMode, message.Password, message.MaximumVisitors, message.CategoryId,
            message.Tags, message.TradeMode, message.AllowPets, message.AllowFoodConsume, message.AllowWalkThrough,
            message.HideWalls, message.WallThickness, message.FloorThickness, message.WhoCanMute, message.WhoCanKick,
            message.WhoCanBan, message.ChatMode, message.ChatBubbleSize, message.ChatScrollUpFrequency,
            message.ChatFullHearRange, message.ChatFloodSensitivity, message.AllowNavigatorDynamicCategories,
            message.LeaveOnDoorTile, message.IdleSleepEnabled, message.IdleSleepTimeoutSeconds,
            message.IdleAutokickEnabled, message.IdleAutokickTimeoutSeconds, message.MuteAllPets));
        return true;
    }
}
