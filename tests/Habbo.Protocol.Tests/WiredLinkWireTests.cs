using System.Buffers;
using Habbo.Protocol.Composers;
using Habbo.Protocol.IO;
using Habbo.Protocol.Messages.Outgoing.Room.Chat;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredLink;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Tests;

/// <summary>The WIRED Link dialog's messages (October 2026), read back in the order the client's wiredlink parsers read
/// them, and the chat bubble a Link speaker stands on a tile.</summary>
public class WiredLinkWireTests
{
    private static MessageReader Written(IComposer composer, string release = ClientReleases.October2026)
    {
        var buffer = new ArrayBufferWriter<byte>();
        composer.Compose(new MessageWriter(buffer, release));
        return new MessageReader(buffer.WrittenSpan.ToArray());
    }

    private static void ReadsNetwork(MessageReader r, string id, string name, int count, bool here)
    {
        Assert.Equal(id, r.ReadString());
        Assert.Equal(name, r.ReadString());
        Assert.Equal(count, r.ReadInt());
        Assert.Equal(here, r.ReadBoolean());
    }

    [Fact]
    public void TheConfigurationNamesTheLinkItsNetworkAndEveryNetwork()
    {
        var r = Written(new WiredLinkConfigurationMessageComposer(5, 99, "12", true,
            [new WiredLinkNetworkData("12", "Lobby", 3, true), new WiredLinkNetworkData("14", "Arena", 0, false)]));

        Assert.Equal(5, r.ReadInt());
        Assert.Equal(99, r.ReadInt());
        Assert.Equal("12", r.ReadString());
        Assert.True(r.ReadBoolean());
        Assert.Equal(2, r.ReadInt());
        ReadsNetwork(r, "12", "Lobby", 3, true);
        ReadsNetwork(r, "14", "Arena", 0, false);
        Assert.Equal(0, r.Remaining);
    }

    [Fact]
    public void AnOperationResultCarriesTheLinksConnectionOnlyWhenItSaysSo()
    {
        var saved = Written(new WiredLinkOperationResultMessageComposer(5, 99, WiredLinkOperationResultMessageComposer.Save, 0, true,
            "12", false, "0", [new WiredLinkNetworkData("12", "Lobby", 1, true)]));
        Assert.Equal([5, 99, 1, 0], Enumerable.Range(0, 4).Select(_ => saved.ReadInt()));
        Assert.True(saved.ReadBoolean());
        Assert.Equal("12", saved.ReadString());
        Assert.False(saved.ReadBoolean());
        Assert.Equal("0", saved.ReadString());
        Assert.Equal(1, saved.ReadInt());
        ReadsNetwork(saved, "12", "Lobby", 1, true);
        Assert.Equal(0, saved.Remaining);

        var failed = Written(WiredLinkOperationResultMessageComposer.Failed(5, 99, WiredLinkOperationResultMessageComposer.Create,
            WiredLinkOperationResultMessageComposer.DuplicateName));
        Assert.Equal([5, 99, 2, 4], Enumerable.Range(0, 4).Select(_ => failed.ReadInt()));
        Assert.False(failed.ReadBoolean());
        Assert.Equal("0", failed.ReadString());
        Assert.Equal(0, failed.ReadInt());
        Assert.Equal(0, failed.Remaining);
    }

    [Fact]
    public void TheConnectionsListEachLinksRoom()
    {
        var r = Written(new WiredLinkConnectionsMessageComposer(5, 99, "12", [new WiredLinkConnectionData(7, "Arena", 120, true, false)]));

        Assert.Equal(5, r.ReadInt());
        Assert.Equal(99, r.ReadInt());
        Assert.Equal("12", r.ReadString());
        Assert.Equal(1, r.ReadInt());
        Assert.Equal(7, r.ReadInt());
        Assert.Equal("Arena", r.ReadString());
        Assert.Equal(120, r.ReadInt());
        Assert.True(r.ReadBoolean());
        Assert.False(r.ReadBoolean());
        Assert.Equal(0, r.Remaining);
    }

    [Fact]
    public void ASpeakersBubbleStandsOnItsTileForOctober2026Only()
    {
        var bubble = ChatMessageComposer.OnTile("ping", 253, 4, 6);

        var october = Written(bubble);
        Assert.Equal(-1, october.ReadInt());
        Assert.Equal("ping", october.ReadString());
        Assert.Equal([0, 253, 0, -1, -1, -1], Enumerable.Range(0, 6).Select(_ => october.ReadInt()));
        Assert.True(october.ReadBoolean());
        Assert.Equal(4, october.ReadInt());
        Assert.Equal(6, october.ReadInt());
        Assert.Equal(0, october.Remaining);

        // an older client reads no further than the bubble width
        var march = Written(bubble, ClientReleases.March2026);
        march.ReadInt();
        march.ReadString();
        Enumerable.Range(0, 6).ToList().ForEach(_ => march.ReadInt());
        Assert.Equal(0, march.Remaining);
    }

    [Fact]
    public void TalkIsNotLocated()
    {
        var talk = new ChatMessageComposer(3, "hi", 0, 0, [], -1, 2, -1);

        var r = Written(talk);
        Assert.Equal(3, r.ReadInt());
        Assert.Equal("hi", r.ReadString());
        Assert.Equal([0, 0, 0, -1, 2, -1], Enumerable.Range(0, 6).Select(_ => r.ReadInt()));
        Assert.Equal(0, r.Remaining);
    }
}
