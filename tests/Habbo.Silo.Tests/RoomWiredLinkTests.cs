using Habbo.Domain.Furniture.Data;
using Habbo.Protocol.Messages.Outgoing.Room.Chat;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;
using Habbo.Silo.Abstractions.Wired;
using Habbo.Silo.Rooms;
using Habbo.Silo.Wired;
using Habbo.Silo.Wired.Effects;
using Habbo.Silo.Wired.Triggers;
using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Tests;

/// <summary>WIRED Links: what the send-link effects send through them, and what the receive-link triggers make of what
/// arrives. Where a Link leads (its network's other rooms, itself) is the grain's; here the room hands its sends over,
/// and is handed what arrives.</summary>
public partial class RoomWiredTests
{
    private static RoomItemStore.PlacedItem Link(Room room, int x, int y)
    {
        var link = room.Furni(x, y, "wf_link", logic: "furniture_multistate");
        link.Item.Definition!.Interaction = Habbo.Silo.Items.Interactions.WiredLinkInteraction.Name;
        return link;
    }

    /// <summary>A link dataframe box's int parameters: the row count, then each row's three (padded to 12 rows).</summary>
    private static List<int> SendRows(params (int Mode, int Literal, int Target)[] rows) =>
        [rows.Length, .. rows.SelectMany(r => new[] { r.Mode, r.Literal, r.Target }), .. new int[(12 - rows.Length) * 3]];

    private static List<string> VariableIds(params string[] ids) =>
        [.. ids, .. Enumerable.Repeat(WiredLinkDataframe.NoVariable, 12 - ids.Length)];

    private static WiredLinkPayload Message(string text, int sourceRoomId = 42, int hops = 0) => new(sourceRoomId, false, text, [], hops);

    [Fact]
    public async Task ACustomMessageGoesOutThroughThePickedLinks()
    {
        RoomItemStore.PlacedItem link = null!;
        var (room, user) = Stack(r =>
        {
            link = Link(r, 6, 0);
            var lamp = r.Furni(7, 0);
            var send = Picks(link, lamp);
            send.IntParams = [SendLinkMessageEffect.CustomMessage];
            send.StringParam = "hello $(who)";
            r.Box(0, 3, "wf_xtra_text_output_username", Addon, new FurniWired { StringParam = "who" });
            r.Box(0, 3, "wf_act_send_message", Effect, send);
        });

        await room.StepAsync(user, 3, 1);

        var (through, payload) = Assert.Single(room.LinkSends);
        Assert.Same(link, through);
        Assert.Equal(new WiredLinkPayload(room.Wired.RoomId, false, "hello tester", [], 0) with { Rows = payload.Rows }, payload);
        Assert.Empty(payload.Rows);
    }

    [Fact]
    public async Task FromTheTriggerSendsWhatWasSaid()
    {
        var room = new Room();
        var link = Link(room, 6, 0);
        room.Box(0, 3, "wf_trg_says_something", Trigger, new FurniWired { IntParams = [0, 2, 0] });
        room.Box(0, 3, "wf_act_send_message", Effect, Picks(link));
        var user = room.User(2, 1);

        await room.Wired.UserSaidAsync(user, "anyone there?");

        Assert.Equal("anyone there?", Assert.Single(room.LinkSends).Payload.Message);
    }

    [Fact]
    public async Task AMessageArrivingOnALinkRunsTheStacksReceivingOnItWithTheTriggerText()
    {
        var room = new Room();
        var link = Link(room, 6, 0);
        var other = Link(room, 7, 0);
        room.Box(0, 3, "wf_trg_receive_message", Trigger, Picks(link));
        room.Box(0, 3, "wf_act_show_message", Effect, new FurniWired { StringParam = "got $(trigger_text)", UserSources = [WiredSources.AllUsers] });
        var user = room.User(2, 1);

        await room.Wired.LinkReceivedAsync(other, Message("not for us"));
        await room.Wired.LinkReceivedAsync(link, Message("ping"));

        var whisper = Assert.Single(room.Sent.Select(s => s.Message).OfType<WhisperMessageComposer>());
        Assert.Equal("got ping", whisper.Text);
        Assert.Equal(user.Player.Id, room.Sent.Single().To);
    }

    [Fact]
    public void TheRoomALinkMessageCameFromIsTheEventsSourceRoom()
    {
        var room = new Room();
        var run = new WiredRun(room.Wired, new WiredEvent(WiredEventType.LinkMessageReceived) { LinkSourceRoomId = 42 });
        var signal = new WiredRun(room.Wired, new WiredEvent(WiredEventType.SignalReceived));

        Assert.Equal(42, room.Wired.VariableValue(Internal(room, "-453"), WiredHolder.Context, run));
        Assert.Null(room.Wired.VariableValue(Internal(room, "-453"), WiredHolder.Context, signal));
    }

    [Fact]
    public async Task AReceivingStackSendingOnCountsAHop()
    {
        var room = new Room();
        var link = Link(room, 6, 0);
        var onward = Link(room, 7, 0);
        room.Box(0, 3, "wf_trg_receive_message", Trigger, Picks(link));
        room.Box(0, 3, "wf_act_send_message", Effect, Picks(onward));

        await room.Wired.LinkReceivedAsync(link, Message("relay", hops: 2));
        await room.Wired.LinkReceivedAsync(link, Message("too far", hops: RoomWired.MaxLinkHops));

        var (through, payload) = Assert.Single(room.LinkSends);
        Assert.Same(onward, through);
        Assert.Equal(("relay", 3), (payload.Message, payload.Hops));
    }

    [Fact]
    public async Task ADataframeSendsEachRowsValue()
    {
        RoomItemStore.PlacedItem link = null!;
        WiredVariable visits = null!;
        var (room, user) = Stack(r =>
        {
            link = Link(r, 6, 0);
            var id = Variable(r, "wf_var_room", new FurniWired { StringParam = "visits", IntParams = [1] });
            visits = r.Wired.FindVariable(id)!;
            var send = Picks(link);
            send.StringParam = """[["score",""],["greeting","hi $(who)"],["visits",""],["missing",""]]""";
            send.IntParams = SendRows(
                (SendLinkDataframeEffect.Number, 5, 0),
                (SendLinkDataframeEffect.CustomMessage, 0, 0),
                (SendLinkDataframeEffect.VariableValue, 0, WiredVariableTargets.Global),
                (SendLinkDataframeEffect.VariableValue, 0, WiredVariableTargets.User));
            send.VariableIds = VariableIds("n", "n", id, id);
            r.Box(0, 3, "wf_xtra_text_output_username", Addon, new FurniWired { StringParam = "who" });
            r.Box(0, 3, "wf_act_send_dataframe", Effect, send);
        });
        await room.Wired.SetVariableAsync(visits, WiredHolder.Room, null, 9);

        await room.StepAsync(user, 3, 1);

        var payload = Assert.Single(room.LinkSends).Payload;
        Assert.True(payload.IsDataframe);
        // the user holds no visits: that row is left out
        Assert.Equal([new WiredLinkRow("score", 5, null), new WiredLinkRow("greeting", null, "hi tester"), new WiredLinkRow("visits", 9, null)],
            payload.Rows);
    }

    [Fact]
    public async Task AReceivedDataframeFillsItsPlaceholdersAndContextVariables()
    {
        var room = new Room();
        var link = Link(room, 6, 0);
        var lamp = room.Furni(7, 0);
        var amount = Variable(room, "wf_var_context", new FurniWired { StringParam = "amount", IntParams = [1] });
        room.Box(0, 3, "wf_trg_receive_dataframe", Trigger, new FurniWired
        {
            StuffIds = [link.Item.Id],
            StringParam = """[["greeting","msg"],["score",""]]""",
            IntParams = [2, ReceiveLinkDataframeTrigger.ToPlaceholder, ReceiveLinkDataframeTrigger.ToVariable, .. new int[10]],
            VariableIds = VariableIds("n", amount),
        });
        room.Box(0, 3, "wf_cnd_has_var", Condition, new FurniWired { VariableIds = [amount], IntParams = [WiredVariableTargets.Context] });
        room.Box(0, 3, "wf_act_show_message", Effect, new FurniWired { StringParam = "says $(msg)", UserSources = [WiredSources.AllUsers] });
        room.Box(0, 3, "wf_act_toggle_state", Effect, Picks(lamp));
        room.User(2, 1);

        await room.Wired.LinkReceivedAsync(link, new WiredLinkPayload(42, true, string.Empty,
            [new WiredLinkRow("greeting", null, "hello"), new WiredLinkRow("score", 12, null)], 0));

        Assert.Equal("1", lamp.Data.State);
        Assert.Equal("says hello", Assert.Single(room.Sent.Select(s => s.Message).OfType<WhisperMessageComposer>()).Text);
    }

    [Fact]
    public async Task ASpeakerShowsWhatArrivedOverItsPicksInItsBubble()
    {
        var room = new Room();
        var link = Link(room, 6, 0);
        var sign = room.Furni(2, 2);
        room.Box(0, 3, "wf_trg_receive_message", Trigger, Picks(link));
        room.Box(0, 3, "wf_trg_receive_dataframe", Trigger, new FurniWired
        {
            StuffIds = [link.Item.Id], IntParams = new int[13].ToList(), VariableIds = VariableIds(),
        });
        var speaker = Picks(sign);
        speaker.IntParams = [1, 0];
        room.Box(0, 3, "wf_xtra_link_speaker", Addon, speaker);

        await room.Wired.LinkReceivedAsync(link, Message("ping"));
        await room.Wired.LinkReceivedAsync(link, new WiredLinkPayload(42, true, string.Empty, [new WiredLinkRow("a", 1, null)], 0));

        var bubble = Assert.Single(room.Broadcasts.OfType<ChatMessageComposer>());
        Assert.Equal(("ping", WiredLinks.SpeakerChatStyle, 2, 2, true), (bubble.Text, bubble.StyleId, bubble.LocationX, bubble.LocationY, bubble.HideUsername));
    }

    [Fact]
    public void LinkBoxesTakeOnlyLinksAndAtLeastOne()
    {
        var room = new Room();
        var link = Link(room, 6, 0);
        var lamp = room.Furni(7, 0);

        foreach (var className in (string[])["wf_act_send_message", "wf_trg_receive_message"])
        {
            var type = Boxes.For(className)!;
            Assert.True(type.PickRule!.Accepts(link));
            Assert.False(type.PickRule.Accepts(lamp));
            Assert.Equal(WiredLinks.RequireLinkFurni, type.Validate(new FurniWired())!.LocalizationKey);
            Assert.Null(type.Validate(Picks(link)));
        }
    }

    [Fact]
    public void ACustomLinkMessageFitsInAThousandBytes()
    {
        var type = Boxes.For("wf_act_send_message")!;
        var settings = new FurniWired { StuffIds = [1], IntParams = [1] };

        settings.StringParam = new string('é', 500);
        Assert.Null(type.Validate(settings));
        settings.StringParam = new string('é', 501);
        Assert.Equal("wiredfurni.params.link.message_too_large", type.Validate(settings)!.LocalizationKey);
    }

    [Theory]
    [InlineData("""[["score",""]]""", 1, null)]
    [InlineData("""[["score",""]]""", 2, WiredLinkDataframe.Invalid)]
    [InlineData("""not json""", 1, WiredLinkDataframe.Invalid)]
    [InlineData("""[["score",""],["Score",""]]""", 2, WiredLinkDataframe.NameNotUnique)]
    [InlineData("""[["no-dash",""]]""", 1, WiredLinkDataframe.NameSyntax)]
    [InlineData("""[["",""]]""", 1, WiredLinkDataframe.KeyLength)]
    public void ADataframesRowsMustBeWhatItsDialogSaves(string rows, int count, string? error)
    {
        var type = Boxes.For("wf_act_send_dataframe")!;
        var settings = new FurniWired
        {
            StuffIds = [1], StringParam = rows, VariableIds = VariableIds(),
            IntParams = [count, .. Enumerable.Repeat(0, 36)],
        };

        Assert.Equal(error, type.Validate(settings)?.LocalizationKey);
    }

    [Fact]
    public void AReceivingDataframeCapturesIntoEachDestinationOnce()
    {
        var type = Boxes.For("wf_trg_receive_dataframe")!;
        var settings = new FurniWired
        {
            StuffIds = [1], StringParam = """[["a","x"],["b","X"]]""", VariableIds = VariableIds(),
            IntParams = [2, .. new int[12]],
        };

        Assert.Equal("wiredfurni.error.link.destination_conflict", type.Validate(settings)!.LocalizationKey);
        settings.StringParam = """[["a","x"],["b","trigger_text"]]""";
        Assert.Equal("wiredfurni.error.placeholder.reserved", type.Validate(settings)!.LocalizationKey);
        settings.StringParam = """[["a","x"],["b","y"]]""";
        Assert.Null(type.Validate(settings));
    }

    [Fact]
    public void ANewDataframeBoxsDialogHasEveryRow()
    {
        var room = new Room();
        var box = room.Box(0, 3, "wf_act_send_dataframe", Effect, new FurniWired());

        var dialog = Assert.IsType<WiredFurniActionComposer>(room.Wired.DialogFor(box.Item.Id));
        Assert.Equal(37, dialog.Box.IntParams.Count);
        Assert.Equal(Enumerable.Repeat("n", 12), dialog.Box.VariableIds);
    }
}
