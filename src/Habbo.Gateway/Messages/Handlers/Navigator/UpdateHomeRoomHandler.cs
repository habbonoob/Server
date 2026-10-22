using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Protocol.Messages.Outgoing.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>179 UpdateHomeRoom — [roomId]. Persists the home room and confirms it with
/// NavigatorSettings (without entering it).</summary>
internal sealed class UpdateHomeRoomHandler : IMessageHandler<UpdateHomeRoomMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, UpdateHomeRoomMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        var homeRoomId = await playerGrain.SetHomeRoomAsync(message.RoomId);
        await context.SendAsync(
            new NavigatorSettingsComposer(homeRoomId, NavigatorSettingsComposer.StayOnHotelView), ct);
        return true;
    }
}
