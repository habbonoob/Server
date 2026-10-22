using Habbo.Protocol.Messages.Incoming.Sound;
using Habbo.Protocol.Messages.Outgoing.Preferences;

namespace Habbo.Gateway.Messages.Handlers.Sound;

/// <summary>2744 GetSoundSettings — answer with the player's preferences. The client asks for
/// the volumes; the event it reads carries every setting, so all of them go back.</summary>
internal sealed class GetSoundSettingsHandler : IMessageHandler<GetSoundSettingsEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, GetSoundSettingsEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await context.SendAsync(AccountPreferencesComposer.From(await playerGrain.GetPreferencesAsync()), ct);
        return true;
    }
}
