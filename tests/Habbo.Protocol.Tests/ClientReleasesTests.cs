using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Hotel;

namespace Habbo.Protocol.Tests;

public class ClientReleasesTests
{
    [Fact]
    public void ReleasesAreOrderedByBuildTime()
    {
        Assert.True(ClientReleases.Compare(ClientReleases.March2026, ClientReleases.September2026) < 0);
        Assert.True(ClientReleases.IsAtLeast(ClientReleases.September2026, ClientReleases.March2026));
        Assert.True(ClientReleases.IsAtLeast(ClientReleases.September2026, ClientReleases.September2026));
        Assert.False(ClientReleases.IsAtLeast(ClientReleases.March2026, ClientReleases.September2026));
    }

    [Fact]
    public void AMessageGoesOutInTheReleasesOwnPayload()
    {
        byte[] native = [1], september = [2];
        var message = new ComposedMessage(1, native, new() { [ClientReleases.September2026] = september });

        Assert.Same(september, message.PayloadFor(ClientReleases.September2026));
        Assert.Same(native, message.PayloadFor(ClientReleases.March2026));
        Assert.Same(native, new ComposedMessage(1, native).PayloadFor(ClientReleases.September2026));
    }
}
