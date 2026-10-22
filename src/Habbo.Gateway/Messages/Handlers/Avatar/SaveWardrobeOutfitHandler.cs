using Habbo.Domain.Users;
using Habbo.Protocol.Messages.Incoming.Avatar;

namespace Habbo.Gateway.Messages.Handlers.Avatar;

/// <summary>SaveWardrobeOutfitMessageEvent — Keeps a look in a wardrobe slot.</summary>
internal sealed class SaveWardrobeOutfitHandler : IMessageHandler<SaveWardrobeOutfitMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SaveWardrobeOutfitMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.SaveOutfitAsync(message.Slot, message.Figure, GenderExtensions.ParseLetter(message.Gender));
        return true;
    }
}
