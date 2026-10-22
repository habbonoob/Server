using System.Globalization;
using Habbo.Protocol.Messages.Outgoing.Advertisement;

namespace Habbo.Silo.Grains.Players;

/// <summary>Ads between rooms: one may be shown when the hotel's interval since the last has passed and the visit has
/// not had its most yet (interstitial.interval and interstitial.max.displays in the client variables).</summary>
public sealed partial class PlayerGrain
{
    private const string InterstitialIntervalVariable = "interstitial.interval";
    private const string InterstitialMaxDisplaysVariable = "interstitial.max.displays";

    private DateTime? _lastInterstitialAt;
    private int _interstitialsShown;

    public async Task SendInterstitialAsync()
    {
        var interval = int.TryParse(await clientVariables.GetAsync(InterstitialIntervalVariable), NumberStyles.Integer, CultureInfo.InvariantCulture, out var ms)
            ? TimeSpan.FromMilliseconds(ms)
            : (TimeSpan?)null;
        var maxDisplays = int.TryParse(await clientVariables.GetAsync(InterstitialMaxDisplaysVariable), NumberStyles.Integer, CultureInfo.InvariantCulture, out var max)
            ? max
            : 0;

        // without both, the hotel has not said how often ads may come, so none do
        var may = interval is { } wait && _interstitialsShown < maxDisplays
                  && (_lastInterstitialAt is not { } last || DateTime.UtcNow - last >= wait);
        Send(new InterstitialMessageComposer(may));
    }

    public Task InterstitialShownAsync()
    {
        _lastInterstitialAt = DateTime.UtcNow;
        _interstitialsShown++;
        return Task.CompletedTask;
    }
}
