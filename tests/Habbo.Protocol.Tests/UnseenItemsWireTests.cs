using System.Buffers;
using Habbo.Protocol.Composers;
using Habbo.Protocol.IO;
using Habbo.Protocol.Messages.Incoming.Notifications;
using Habbo.Protocol.Messages.Outgoing.Notifications;

namespace Habbo.Protocol.Tests;

/// <summary>The unseen set is what puts the count on the inventory button, so its shape has to
/// match UnseenItemsParser exactly: a count of categories, then each category's own id list.</summary>
public class UnseenItemsWireTests
{
    private static MessageReader Compose(IComposer composer)
    {
        var buffer = new ArrayBufferWriter<byte>();
        composer.Compose(new MessageWriter(buffer));
        return new MessageReader(buffer.WrittenMemory);
    }

    [Fact]
    public void UnseenItemsNestsAnIdListInsideEachCategory()
    {
        var reader = Compose(UnseenItemsComposer.OwnedFurni([41, 42, 43]));

        Assert.Equal(1, reader.ReadInt());                                  // one category
        Assert.Equal(UnseenItemsComposer.OwnedFurniCategory, reader.ReadInt());
        Assert.Equal(3, reader.ReadInt());                                  // three ids, as the inventory's strip ids
        Assert.Equal(-41, reader.ReadInt());
        Assert.Equal(-42, reader.ReadInt());
        Assert.Equal(-43, reader.ReadInt());
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void AnEmptyCategoryStillWritesItsCount()
    {
        // A zero count is how the client is told a category has nothing unseen; writing no
        // count at all would leave it reading the next field as one.
        var reader = Compose(UnseenItemsComposer.OwnedFurni([]));

        Assert.Equal(1, reader.ReadInt());
        Assert.Equal(UnseenItemsComposer.OwnedFurniCategory, reader.ReadInt());
        Assert.Equal(0, reader.ReadInt());
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void ResetUnseenItemIdsReadsItsIdList()
    {
        // The generated reader had no way to read a list until now; it read the count as the
        // whole field and left the ids in the buffer.
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessageWriter(buffer);
        writer.WriteInt(1);
        writer.WriteInt(2);
        writer.WriteInt(77);
        writer.WriteInt(88);

        var message = ResetUnseenItemIdsEvent.Read(new MessageReader(buffer.WrittenMemory));

        Assert.Equal(1, message.Category);
        Assert.Equal([77, 88], message.ItemIds);
    }

    [Fact]
    public void ResetUnseenItemsReadsItsCategory()
    {
        var buffer = new ArrayBufferWriter<byte>();
        new MessageWriter(buffer).WriteInt(5);

        Assert.Equal(5, ResetUnseenItemsEvent.Read(new MessageReader(buffer.WrittenMemory)).Category);
    }
}
