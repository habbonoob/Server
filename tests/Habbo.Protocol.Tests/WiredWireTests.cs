using System.Buffers;
using Habbo.Protocol.IO;
using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu;

namespace Habbo.Protocol.Tests;

/// <summary>The wired dialog messages, read back in exactly the order the client's parsers read
/// them (Triggerable and its subclasses; the Update*MessageComposer classes for what it sends).
/// A field out of place shifts every field after it, so each test ends with nothing left over.</summary>
public class WiredWireTests
{
    private static MessageReader Written(Composers.IComposer composer)
    {
        var buffer = new ArrayBufferWriter<byte>();
        composer.Compose(new MessageWriter(buffer));
        return new MessageReader(buffer.WrittenMemory);
    }

    private static WiredBoxData Box() => new(
        FurniLimit: 20,
        StuffIds: [11, 12],
        StuffIds2: [13],
        StuffTypeId: 4077,
        ItemId: 99,
        StringParam: "hello",
        IntParams: [1, 0, 1],
        VariableIds: ["v1"],
        FurniSources: [100],
        UserSources: [0],
        Code: 2);

    private static List<int> Ints(MessageReader r)
    {
        var count = r.ReadInt();
        return Enumerable.Range(0, count).Select(_ => r.ReadInt()).ToList();
    }

    /// <summary>Triggerable's constructor up to the type's own fields.</summary>
    private static void ReadBox(MessageReader r)
    {
        Assert.Equal(20, r.ReadInt());
        Assert.Equal([11, 12], Ints(r));
        Assert.Equal([13], Ints(r));
        Assert.Equal(4077, r.ReadInt());
        Assert.Equal(99, r.ReadInt());
        Assert.Equal("hello", r.ReadString());
        Assert.Equal([1, 0, 1], Ints(r));
        Assert.Equal(1, r.ReadInt());
        Assert.Equal("v1", r.ReadString());
        Assert.Equal([100], Ints(r));
        Assert.Equal([0], Ints(r));
        Assert.Equal(2, r.ReadInt());
    }

    /// <summary>advancedMode, InputSourcesConf, allowWallFurni.</summary>
    private static void ReadOptions(MessageReader r)
    {
        Assert.False(r.ReadBoolean());
        Assert.Equal(1, r.ReadInt()); // furni slots
        Assert.Equal([100, 200], Ints(r));
        Assert.Equal(0, r.ReadInt()); // user slots
        Assert.Equal([100], Ints(r)); // default furni sources
        Assert.Equal([], Ints(r)); // default user sources
        Assert.False(r.ReadBoolean());
    }

    /// <summary>WiredContext, then the default int params.</summary>
    private static void ReadContext(MessageReader r)
    {
        Assert.Equal(0, r.ReadInt());
        Assert.Equal([0, 1], Ints(r));
    }

    private static readonly WiredBoxOptionsData Options = new(false, [new([100, 200])], [], [100], [], false);
    private static readonly WiredBoxContextData Context = new([], [0, 1]);

    [Fact]
    public void AConditionDialogIsTheBoxItsQuantifierTheOptionsItsTypeAndTheContext()
    {
        var r = Written(new WiredFurniConditionComposer(Box(), Quantifier: 1, Options, QuantifierType: 0, IsInvert: false, Context));

        ReadBox(r);
        Assert.Equal(1, r.ReadInt()); // readDefinitionSpecifics: quantifier code
        ReadOptions(r);
        Assert.Equal(0, r.ReadByte()); // readTypeSpecifics: quantifier type
        Assert.False(r.ReadBoolean()); // and invert
        ReadContext(r);
        Assert.Equal(0, r.Remaining);
    }

    [Fact]
    public void AnEffectDialogCarriesItsDelayAfterTheBox()
    {
        var r = Written(new WiredFurniActionComposer(Box(), DelayInPulses: 6, Options, Context));

        ReadBox(r);
        Assert.Equal(6, r.ReadInt());
        ReadOptions(r);
        ReadContext(r);
        Assert.Equal(0, r.Remaining);
    }

    [Fact]
    public void ASelectorDialogCarriesFilterAndInvertAfterTheBox()
    {
        var r = Written(new WiredFurniSelectorComposer(Box(), IsFilter: true, IsInvert: false, Options, Context));

        ReadBox(r);
        Assert.True(r.ReadBoolean());
        Assert.False(r.ReadBoolean());
        ReadOptions(r);
        ReadContext(r);
        Assert.Equal(0, r.Remaining);
    }

    [Fact]
    public void ATriggerDialogHasNothingOfItsOwn()
    {
        var r = Written(new WiredFurniTriggerComposer(Box(), Options, Context));

        ReadBox(r);
        ReadOptions(r);
        ReadContext(r);
        Assert.Equal(0, r.Remaining);
    }

    [Fact]
    public void SavedMeansAnEmptyMessage()
    {
        var r = Written(new WiredSaveSuccessComposer());

        Assert.Equal(0, r.Remaining);
    }

    [Fact]
    public void AnEffectSaveIsReadInTheOrderTheClientWritesIt()
    {
        // UpdateActionMessageEvent: id, ints, string, stuff ids, delay, furni sources,
        // user sources, variable ids, second stuff ids
        var buffer = new ArrayBufferWriter<byte>();
        var w = new MessageWriter(buffer);
        w.WriteInt(99);
        w.WriteInt(2); w.WriteInt(5); w.WriteInt(7);
        w.WriteString("text");
        w.WriteInt(1); w.WriteInt(11);
        w.WriteInt(4);
        w.WriteInt(1); w.WriteInt(100);
        w.WriteInt(1); w.WriteInt(0);
        w.WriteInt(1); w.WriteString("v1");
        w.WriteInt(1); w.WriteInt(13);
        var r = new MessageReader(buffer.WrittenMemory);

        var save = UpdateActionMessageEvent.Read(r);

        Assert.Equal(99, save.ItemId);
        Assert.Equal([5, 7], save.IntParams);
        Assert.Equal("text", save.StringParam);
        Assert.Equal([11], save.StuffIds);
        Assert.Equal(4, save.DelayInPulses);
        Assert.Equal([100], save.FurniSources);
        Assert.Equal([0], save.UserSources);
        Assert.Equal(["v1"], save.VariableIds);
        Assert.Equal([13], save.StuffIds2);
        Assert.Equal(0, r.Remaining);
    }

    [Fact]
    public void WiredMovementsAreEachTheirKindThenTheirFields()
    {
        var r = Written(new WiredMovementsMessageComposer([
            new WiredFurniMoveData(1, 2, 3, 4, "0.0", "0.5", 77, 500, 2),
            new WiredUserMoveData(5, 6, 7, 8, "1.0", "1.5", 3, WiredUserMoveData.Slide, 250, 4, 5),
            new WiredUserDirectionData(3, 6, 7),
        ]));

        Assert.Equal(3, r.ReadInt());

        // WiredMovementsMessageParser.parseFurniMove: from, to, heights as strings, id, time, rotation
        Assert.Equal(WiredMovementData.FurniMoveKind, r.ReadInt());
        Assert.Equal([1, 2, 3, 4], new[] { r.ReadInt(), r.ReadInt(), r.ReadInt(), r.ReadInt() });
        Assert.Equal(("0.0", "0.5"), (r.ReadString(), r.ReadString()));
        Assert.Equal([77, 500, 2], new[] { r.ReadInt(), r.ReadInt(), r.ReadInt() });

        // parseUserMove: from, to, heights, index, 1 for a slide, time, body and head directions
        Assert.Equal(WiredMovementData.UserMoveKind, r.ReadInt());
        Assert.Equal([5, 6, 7, 8], new[] { r.ReadInt(), r.ReadInt(), r.ReadInt(), r.ReadInt() });
        Assert.Equal(("1.0", "1.5"), (r.ReadString(), r.ReadString()));
        Assert.Equal([3, 1, 250, 4, 5], new[] { r.ReadInt(), r.ReadInt(), r.ReadInt(), r.ReadInt(), r.ReadInt() });

        // parseUserDirUpdate
        Assert.Equal(WiredMovementData.UserDirectionKind, r.ReadInt());
        Assert.Equal([3, 6, 7], new[] { r.ReadInt(), r.ReadInt(), r.ReadInt() });
        Assert.Equal(0, r.Remaining);
    }

    [Fact]
    public void TheClickUserMessagesMatchTheClientsParsers()
    {
        var environment = Written(new WiredEnvironmentMessageComposer(HasClickUserWired: true, EnabledAchievements: ["ACH_RoomEntry"]));
        Assert.True(environment.ReadBoolean());
        Assert.Equal(1, environment.ReadInt());
        Assert.Equal("ACH_RoomEntry", environment.ReadString());
        Assert.Equal(0, environment.Remaining);

        var response = Written(new WiredClickUserResponseComposer(RoomIndex: 4, OpenMenu: false));
        Assert.Equal(4, response.ReadInt());
        Assert.False(response.ReadBoolean());
        Assert.Equal(0, response.Remaining);
    }

    [Fact]
    public void TheInspectionDataBranchesOnWhatIsInspected()
    {
        // WiredObjectInspectionData: a furni has its id and the boxes picking it; the room has neither
        var furni = Written(new WiredVariablesForObjectComposer(0, 42, null, [new WiredVariableValueData("7", 3)], [9]));
        Assert.Equal(0, furni.ReadInt());
        Assert.Equal(42, furni.ReadInt());
        Assert.Equal(1, furni.ReadInt());
        Assert.Equal(("7", 3), (furni.ReadString(), furni.ReadInt()));
        Assert.Equal([9], Ints(furni));
        Assert.Equal(0, furni.Remaining);

        var room = Written(new WiredVariablesForObjectComposer(-10, null, null, [], null));
        Assert.Equal(-10, room.ReadInt());
        Assert.Equal(0, room.ReadInt());
        Assert.Equal(0, room.Remaining);
    }

    [Fact]
    public void AVariableCarriesItsTextConnectorOnlyWhenItHasOne()
    {
        var entry = WiredContextEntryData.RoomVariable(
            new WiredVariableData("5", 0, "score", 1, -10, true, false, true, true, true, false, true, true, false, null), 12);
        var r = Written(new WiredFurniVariableComposer(Box(),
            new WiredBoxOptionsData(false, [], [], [], [], false), new WiredBoxContextData([entry], [])));
        ReadBox(r);
        r.ReadBoolean(); r.ReadInt(); r.ReadInt(); r.ReadInt(); r.ReadInt(); r.ReadBoolean(); // options
        Assert.Equal(1, r.ReadInt());
        Assert.Equal(WiredContextEntryData.RoomVariableType, r.ReadInt());
        Assert.Equal("5", r.ReadString());
        Assert.Equal(0, r.ReadInt());
        Assert.Equal("score", r.ReadString());
        Assert.Equal(1, r.ReadInt());
        Assert.Equal(-10, r.ReadInt());
        for (var flag = 0; flag < 8; flag++)
            r.ReadBoolean();
        Assert.False(r.ReadBoolean()); // no text connector, so nothing follows it
        Assert.Equal(12, r.ReadInt());
        Assert.Equal(0, r.ReadInt()); // default int params
        Assert.Equal(0, r.Remaining);
    }
}
