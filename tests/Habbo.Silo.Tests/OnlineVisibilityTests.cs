using Habbo.Domain.Users;
using Habbo.Silo.Users;

namespace Habbo.Silo.Tests;

public class OnlineVisibilityTests
{
    [Theory]
    [InlineData(OnlineIndicatorPreference.Everyone, false, false, false)]
    [InlineData(OnlineIndicatorPreference.Nobody, false, true, true)]
    [InlineData(OnlineIndicatorPreference.RelationshipStatus, false, false, true)]
    [InlineData(OnlineIndicatorPreference.RelationshipStatus, false, true, false)]
    public void APlayerHidesAsTheirPreferenceSays(OnlineIndicatorPreference preference, bool self, bool related, bool hides) =>
        Assert.Equal(hides, OnlineVisibility.Hides(preference, self, related));

    [Theory]
    [InlineData(OnlineIndicatorPreference.Nobody)]
    [InlineData(OnlineIndicatorPreference.RelationshipStatus)]
    public void NobodyHidesFromThemselves(OnlineIndicatorPreference preference) =>
        Assert.False(OnlineVisibility.Hides(preference, viewerIsSelf: true, viewerHasRelationship: false));
}
