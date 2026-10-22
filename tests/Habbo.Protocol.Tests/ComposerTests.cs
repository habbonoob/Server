using System.Buffers;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Habbo.Protocol.Composers;
using Habbo.Protocol.IO;
using Habbo.Protocol.Messages.Outgoing.Room.Chat;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Silo.Abstractions.Chat;
using Habbo.Silo.Abstractions.Rooms;

namespace Habbo.Protocol.Tests;

/// <summary>Byte-level checks for layouts the client reads in a way that is easy to get subtly
/// wrong — a count of the wrong width still produces a well-formed packet.</summary>
public class ComposerTests
{
    private static MessageReader Compose(IComposer composer)
    {
        var buffer = new ArrayBufferWriter<byte>();
        composer.Compose(new MessageWriter(buffer));
        return new MessageReader(buffer.WrittenMemory);
    }

    [Fact]
    public void AChatCopyNamesItsListenerAndTracksOnlyForTheSpeaker()
    {
        // Habbo, from a listener's side: ... trackingId -1, receiverRoomIndex = the listener's own index, width -1
        var message = new ChatMessage(RoomIndex: 3, "hi", Gesture.None, StyleId: 0, Links: [], TrackingId: 7);

        var heard = Compose(ChatMessageComposer.From(message, receiverRoomIndex: 9, toSpeaker: false));
        Assert.Equal(3, heard.ReadInt());
        Assert.Equal("hi", heard.ReadString());
        Assert.Equal([0, 0, 0], new[] { heard.ReadInt(), heard.ReadInt(), heard.ReadInt() });
        Assert.Equal([-1, 9, -1], new[] { heard.ReadInt(), heard.ReadInt(), heard.ReadInt() });
        Assert.Equal(0, heard.Remaining);

        var said = Compose(ChatMessageComposer.From(message, receiverRoomIndex: 3, toSpeaker: true));
        said.ReadInt();
        said.ReadString();
        Assert.Equal([0, 0, 0, 7, 3, -1], Enumerable.Range(0, 6).Select(_ => said.ReadInt()));
    }

    [Fact]
    public void HeightMapUpdateCountsTilesInASingleByte()
    {
        // HeightMapUpdateMessageParser reads the count with readByte(); an int count would be
        // read as 0 and the whole update silently dropped.
        var tiles = new HeightMapTileData[]
        {
            new(3, 4, 1.0, StackingBlocked: false),
            new(5, 6, 2.5, StackingBlocked: true),
        };

        var reader = Compose(HeightMapUpdateMessageComposer.From(tiles));

        Assert.Equal(tiles.Length, reader.ReadByte());

        Assert.Equal(3, reader.ReadByte());
        Assert.Equal(4, reader.ReadByte());
        Assert.Equal((short)(1.0 * 256), reader.ReadShort());

        Assert.Equal(5, reader.ReadByte());
        Assert.Equal(6, reader.ReadByte());
        var blocked = reader.ReadShort();
        Assert.Equal(1, (blocked >> HeightMapMessageComposer.StackingBlockedBit) & 1);
        // the top bit, as room.stacking_blocked_mask_bit=15 has the client read it (Habbo: a blocked 0.45 is 0x8073)
        Assert.Equal(unchecked((short)0x8280), blocked);
    }

    [Fact]
    public void MoreTilesThanASignedByteCountsGoAsSeveralUpdates()
    {
        // the client's readByte() is signed: 128 tiles would read as -128 and the parser run off the end
        var tiles = Enumerable.Range(0, 300).Select(i => new HeightMapTileData(i % 60, i / 60, 0, StackingBlocked: false)).ToArray();

        var counts = HeightMapUpdateMessageComposer.Chunked(tiles).Select(update => (int)(sbyte)Compose(update).ReadByte()).ToList();

        Assert.Equal([127, 127, 46], counts);
        Assert.Throws<ArgumentException>(() => HeightMapUpdateMessageComposer.From(tiles));
    }

    [Fact]
    public void HeightMapCountsTilesAsWidthTimesHeight()
    {
        // The client derives the room height by dividing this count by the width, so the count
        // has to be the total tile count rather than the number of rows.
        const int width = 3;
        const int height = 2;

        var tiles = Enumerable.Range(0, width * height)
            .Select(i => new HeightMapTileData(i % width, i / width, 0, StackingBlocked: false))
            .ToList();

        var reader = Compose(HeightMapMessageComposer.From(width, height, tiles));

        Assert.Equal(width, reader.ReadInt());
        Assert.Equal(width * height, reader.ReadInt());
    }

    [Fact]
    public void AnEntityWithoutUserDetailsStopsAfterItsType()
    {
        // Bots and pets end where a user's trailing fields would begin.
        var bot = new RoomEntityWireData(
            AccountId: 7, Name: "Bot", Motto: "", Figure: "hr-1",
            RoomIndex: 2, X: 1, Y: 1, Z: "0.0", HeadDirection: 4,
            EntityType: (int)RoomEntityType.Bot, User: null, Bot: null, Pet: null);

        var reader = Compose(new UsersMessageComposer([bot]));

        Assert.Equal(1, reader.ReadInt());
        Assert.Equal(7, reader.ReadInt());
        Assert.Equal("Bot", reader.ReadString());
        Assert.Equal("", reader.ReadString());
        Assert.Equal("hr-1", reader.ReadString());
        Assert.Equal(2, reader.ReadInt());
        Assert.Equal(1, reader.ReadInt());
        Assert.Equal(1, reader.ReadInt());
        Assert.Equal("0.0", reader.ReadString());
        Assert.Equal(4, reader.ReadInt());
        Assert.Equal((int)RoomEntityType.Bot, reader.ReadInt());

        // nothing follows
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.ReadInt());
    }

    [Fact]
    public void ABotWithSkillsCarriesItsOwnerAndSkillsAsShorts()
    {
        var bot = RoomEntityWireData.From(new RoomEntityData(
            RoomIndex: 3, AccountId: 12, Name: "Bartender", Motto: "", Figure: "hd-600-1", Sex: "F",
            X: 2, Y: 2, Z: "0.0", HeadDirection: 2, RoomEntityType.BotWithSkills,
            GroupId: 0, GroupStatus: 0, GroupName: "", SwimFigure: "", AchievementScore: 0, IsModerator: false,
            OwnerAccountId: 5, OwnerName: "tester", BotSkills: [1, 6]));

        var reader = Compose(new UsersMessageComposer([bot]));

        Assert.Equal(1, reader.ReadInt());
        Assert.Equal(12, reader.ReadInt());
        for (var i = 0; i < 3; i++)
            reader.ReadString();
        for (var i = 0; i < 3; i++)
            reader.ReadInt();
        reader.ReadString();
        reader.ReadInt();
        Assert.Equal((int)RoomEntityType.BotWithSkills, reader.ReadInt());
        Assert.Equal("F", reader.ReadString());
        Assert.Equal(5, reader.ReadInt());
        Assert.Equal("tester", reader.ReadString());
        Assert.Equal(2, reader.ReadInt());
        Assert.Equal(1, reader.ReadShort());
        Assert.Equal(6, reader.ReadShort());
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.ReadInt());
    }
}
