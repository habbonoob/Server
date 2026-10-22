using System.Reflection;
using Habbo.Protocol.Releases;

namespace Habbo.Protocol.Tests;

public class ReleaseHeadersTests
{
    private const string Newer = "WIN63-202609161723-93809945";

    private static HashSet<ushort> Constants(Type type)
        => type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(ushort))
            .Select(f => (ushort)f.GetRawConstantValue()!)
            .ToHashSet();

    [Fact]
    public void TheServersHeadersAreTheNativeTablesAndServerIdsForLaterOnes()
    {
        var native = ReleaseHeaders.Find(ReleaseHeaders.Native);
        Assert.NotNull(native);

        // every native message is declared, and every declared one is a message some release has
        Assert.Subset(Constants(typeof(ClientMessages)), native.Composers.Values.ToHashSet());
        Assert.Subset(Constants(typeof(ServerMessages)), native.Events.Values.ToHashSet());
        Assert.Subset(ServerHeaders.All.Composers.Values.ToHashSet(), Constants(typeof(ClientMessages)));
        Assert.Subset(ServerHeaders.All.Events.Values.ToHashSet(), Constants(typeof(ServerMessages)));
    }

    [Fact]
    public void EveryReleaseSaysHelloAt4000()
    {
        Assert.NotEmpty(ReleaseHeaders.All);
        foreach (var table in ReleaseHeaders.All.Values)
            Assert.Equal(ClientMessages.ClientHelloMessageEvent, table.Composers["ClientHelloMessageComposer"]);
    }

    [Fact]
    public void TheNativeReleaseIsNotTranslated()
    {
        var map = ReleaseHeaderMap.For(ReleaseHeaders.Native);
        Assert.Same(ReleaseHeaderMap.Native, map);
        Assert.True(map.TryIncoming(ClientMessages.InfoRetrieveMessageEvent, out var incoming));
        Assert.Equal(ClientMessages.InfoRetrieveMessageEvent, incoming);
        Assert.True(map.TryOutgoing(ServerMessages.CustomStackingHeightUpdateMessageComposer, out var outgoing));
        Assert.Equal(ServerMessages.CustomStackingHeightUpdateMessageComposer, outgoing);
    }

    [Fact]
    public void ALaterReleasesOwnMessagesGetServerIdsAboveEveryClientId()
    {
        var newer = ReleaseHeaders.Find(Newer)!;
        var server = ServerHeaders.All;
        var header = newer.Composers["GetRaidProtectionSettingsComposer"];

        Assert.Equal(ServerHeaders.ReleaseStride + header, server.Composers["GetRaidProtectionSettingsComposer"]);
        Assert.True(ReleaseHeaderMap.For(Newer)!.TryIncoming(header, out var incoming));
        Assert.Equal(ServerHeaders.ReleaseStride + header, incoming);

        // the native client never has it sent
        var raid = server.Events["RaidProtectionSettingsEvent"];
        Assert.False(ReleaseHeaderMap.Native.TryOutgoing(raid, out _));
        Assert.True(ReleaseHeaderMap.For(Newer)!.TryOutgoing(raid, out var outgoing));
        Assert.Equal(newer.Events["RaidProtectionSettingsEvent"], outgoing);
    }

    [Fact]
    public void ServerIdsAreUnique()
    {
        Assert.Equal(ServerHeaders.All.Composers.Count, ServerHeaders.All.Composers.Values.Distinct().Count());
        Assert.Equal(ServerHeaders.All.Events.Count, ServerHeaders.All.Events.Values.Distinct().Count());
    }

    [Fact]
    public void AnUnknownReleaseHasNoMap() => Assert.Null(ReleaseHeaderMap.For("WIN63-000000000000-0"));

    [Fact]
    public void ANewerReleaseIsTranslatedByName()
    {
        var newer = ReleaseHeaders.Find(Newer)!;
        var map = ReleaseHeaderMap.For(Newer)!;

        Assert.True(map.TryIncoming(newer.Composers["InfoRetrieveMessageComposer"], out var incoming));
        Assert.Equal(ClientMessages.InfoRetrieveMessageEvent, incoming);

        Assert.True(map.TryOutgoing(ServerMessages.UserObjectComposer, out var outgoing));
        Assert.Equal(newer.Events["UserObjectEvent"], outgoing);
    }

    [Fact]
    public void AMessageOnlyOneReleaseHasIsNotTranslated()
    {
        var newer = ReleaseHeaders.Find(Newer)!;
        var map = ReleaseHeaderMap.For(Newer)!;

        // gone from the newer client: never sent to it
        Assert.False(newer.Events.ContainsKey("CustomStackingHeightUpdateMessageEvent"));
        Assert.False(map.TryOutgoing(ServerMessages.CustomStackingHeightUpdateMessageComposer, out _));

        // a message the newer client lacks, unknown to it: never sent
        Assert.False(map.TryOutgoing(ServerMessages.FigureSetIdAddedComposer, out _));
    }
}
