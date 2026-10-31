using System.Buffers;
using Habbo.Protocol.Composers;
using Habbo.Protocol.IO;
using Habbo.Protocol.Messages.Outgoing.FriendList;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Tests;

/// <summary>A console message carries text or (September 2026) a Habbicon; an older client reads only text, and is
/// given the Habbicon's name.</summary>
public class ConsoleHabbiconWireTests
{
    private static MessageReader Compose(IComposer composer, string release)
    {
        var buffer = new ArrayBufferWriter<byte>();
        composer.Compose(new MessageWriter(buffer, release));
        return new MessageReader(buffer.WrittenSpan.ToArray());
    }

    private static NewConsoleMessageComposer Habbicon =>
        new(7, ConsoleMessageType.Habbicon, "duck_happy", 0, "key", 3, 7, "Bob", "hd-180", habbiconId: 29);

    [Fact]
    public void SeptemberReadsTheHabbiconsIdInPlaceOfText()
    {
        var reader = Compose(Habbicon, ClientReleases.September2026);

        Assert.Equal(7, reader.ReadInt());
        Assert.Equal((int)ConsoleMessageType.Habbicon, reader.ReadInt());
        Assert.Equal(29, reader.ReadInt());
        Assert.Equal(0, reader.ReadInt());
        Assert.Equal("key", reader.ReadString());
    }

    [Fact]
    public void MarchReadsTheHabbiconsName()
    {
        var reader = Compose(Habbicon, ClientReleases.March2026);

        Assert.Equal(7, reader.ReadInt());
        Assert.Equal("duck_happy", reader.ReadString());
        Assert.Equal(0, reader.ReadInt());
        Assert.Equal("key", reader.ReadString());
    }

    [Fact]
    public void TextIsTextForEitherRelease()
    {
        var text = new NewConsoleMessageComposer(7, ConsoleMessageType.Text, "hi", 0, "key", 3, 7, "Bob", "hd-180");

        var september = Compose(text, ClientReleases.September2026);
        Assert.Equal(7, september.ReadInt());
        Assert.Equal((int)ConsoleMessageType.Text, september.ReadInt());
        Assert.Equal("hi", september.ReadString());
        Assert.Equal(0, september.ReadInt());

        var march = Compose(text, ClientReleases.March2026);
        Assert.Equal(7, march.ReadInt());
        Assert.Equal("hi", march.ReadString());
        Assert.Equal(0, march.ReadInt());
    }
}
