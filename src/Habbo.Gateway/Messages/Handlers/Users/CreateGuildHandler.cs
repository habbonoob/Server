using Habbo.Protocol.Messages.Incoming.Users;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>CreateGuildMessageEvent — buys a group. The price comes off the player's credits, so
/// the session's copy of the player is refreshed after.</summary>
internal sealed class CreateGuildHandler(IGroupService groups) : IMessageHandler<CreateGuildMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, CreateGuildMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.PlayerGrain is not { } playerGrain)
            return false;

        var draft = new GroupDraft(
            message.Name, message.Description, message.RoomId, message.PrimaryColorId, message.SecondaryColorId, message.BadgeData);
        if (await groups.CreateAsync(player.Id, draft))
            context.Session.RefreshPlayer(await playerGrain.GetDataAsync());
        return true;
    }
}
