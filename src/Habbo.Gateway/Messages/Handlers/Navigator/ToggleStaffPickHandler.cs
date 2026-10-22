using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>ToggleStaffPickMessageEvent — staff pick a room, or unpick it.</summary>
internal sealed class ToggleStaffPickHandler(IRoomDirectoryService service) : IMessageHandler<ToggleStaffPickMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ToggleStaffPickMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.ToggleStaffPickAsync(player, message.FlatId, message.StaffPick);
        return true;
    }
}
