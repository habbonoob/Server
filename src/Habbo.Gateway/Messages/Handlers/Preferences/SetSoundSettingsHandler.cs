using Habbo.Protocol.Messages.Incoming.Preferences;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Gateway.Messages.Handlers.Preferences;

/// <summary>2565 SetSoundSettings — the client pushes trax, furni, UI volume in that order
/// (the reverse of the preferences record's parameters).</summary>
internal sealed class SetSoundSettingsHandler : IMessageHandler<SetSoundSettingsEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, SetSoundSettingsEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.UpdatePreferencesAsync(new PreferencesUpdate(
            UiVolume: message.UiVolume,
            FurniVolume: message.FurniVolume,
            TraxVolume: message.TraxVolume));
        return true;
    }
}
