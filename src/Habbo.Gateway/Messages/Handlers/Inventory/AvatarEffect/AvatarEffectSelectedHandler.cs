using Habbo.Protocol.Messages.Incoming.Inventory.AvatarEffect;

namespace Habbo.Gateway.Messages.Handlers.Inventory.AvatarEffect;

/// <summary>AvatarEffectSelectedEvent — wears one of the player's running effects, or takes the
/// worn one off (-1).</summary>
internal sealed class AvatarEffectSelectedHandler : IMessageHandler<AvatarEffectSelectedEvent>
{
    /// <summary>What the client sends to take the effect off.</summary>
    private const int Deselect = -1;

    /// <summary>The effect id the room reads as none.</summary>
    private const int NoEffect = 0;

    public async Task<bool> HandleAsync(GatewaySessionContext context, AvatarEffectSelectedEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.PlayerGrain is not { } playerGrain)
            return false;

        if (message.Type != Deselect && !await playerGrain.CanWearEffectAsync(message.Type))
            return true;

        if (context.Session.Room is { } room)
            await room.WearEffectAsync(player.Id, message.Type == Deselect ? NoEffect : message.Type);
        await playerGrain.EffectSelectedAsync(message.Type);
        return true;
    }
}
