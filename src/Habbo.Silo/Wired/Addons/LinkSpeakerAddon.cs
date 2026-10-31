using System.Globalization;

namespace Habbo.Silo.Wired.Addons;

/// <summary>The WIRED Link speaker (wf_xtra_link_speaker): in a stack that receives on WIRED Links, it shows what
/// arrived — a message, a dataframe, or both, as its dialog's "speak for" ticks — as a chat bubble in the link
/// speaker's style (253), standing over each of its picked furni, or over the Link it arrived on when it picked none.
/// A dataframe reads as its rows, "key: value", one after another.
///
/// A guess: the client has the dialog, the bubble style and bubbles that stand on a tile said by no one, but nothing
/// says what Habbo shows, where, or when; a packet capture should settle it.</summary>
[WiredBox("wf_xtra_link_speaker")]
public sealed class LinkSpeakerAddon : WiredAddon
{
    private const int SpeaksForMessages = 0;
    private const int SpeaksForDataframes = 1;
    private const string RowSeparator = ", ";

    public override int Code => AddonCodes.LinkSpeaker;
    public override bool PicksFurni => true;

    /// <summary>Shows what arrived on the Link the stack's trigger heard, if the speaker speaks for it.</summary>
    public static void Speak(WiredBox speaker, WiredRun run)
    {
        var text = run.Event switch
        {
            { Type: WiredEventType.LinkMessageReceived, Text: { } message } when speaker.Flag(SpeaksForMessages) => message,
            { Type: WiredEventType.LinkDataframeReceived } e when speaker.Flag(SpeaksForDataframes) => string.Join(RowSeparator,
                e.LinkRows.Select(row => row.Key + ": " + (row.Text ?? row.Number?.ToString(CultureInfo.InvariantCulture)))),
            _ => null,
        };
        if (string.IsNullOrEmpty(text))
            return;

        var over = run.Room.Picked(speaker).ToList();
        if (over.Count == 0 && run.Item is { } link)
            over.Add(link);
        foreach (var item in over)
            run.Room.SpeakAt(item, text, WiredLinks.SpeakerChatStyle);
    }
}
