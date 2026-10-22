using Habbo.Protocol.Messages.Incoming.Inventory.AvatarEffect;

namespace Habbo.Gateway.Messages.Handlers.Inventory.AvatarEffect;

/// <summary>AvatarEffectActivatedEvent — Starts one of the effects waiting in the inventory.</summary>
internal sealed class AvatarEffectActivatedHandler : IMessageHandler<AvatarEffectActivatedEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, AvatarEffectActivatedEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.ActivateEffectAsync(message.Type);
        return true;
    }
}
