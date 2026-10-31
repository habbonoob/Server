using Habbo.Domain.Furniture.Data;

namespace Habbo.Silo.Wired.Effects;

/// <summary>Sends a message through its WIRED Links, to the stacks receiving on the Links of their networks in other
/// rooms (and on a Link itself when it receives its own room's): the trigger's text — what was said, or the link message
/// received — or a custom message with the stack's placeholders filled in, of up to 1000 bytes. Nothing is sent for a
/// trigger that heard no text.</summary>
[WiredBox("wf_act_send_message")]
public sealed class SendLinkMessageEffect : WiredEffect
{
    private const int Source = 0;

    /// <summary>The dialog's message sources (wiredfurni.params.link.from_trigger, .custom_message).</summary>
    public const int FromTrigger = 0;
    public const int CustomMessage = 1;

    /// <summary>The longest custom message, in UTF-8 bytes (wiredfurni.params.link.message_too_large).</summary>
    public const int MaxMessageBytes = 1000;

    public override int Code => EffectCodes.SendLinkMessage;
    public override bool PicksFurni => true;
    public override WiredPickRule? PickRule => WiredLinks.PickRule;
    public override IReadOnlyList<int> DefaultIntParams => [FromTrigger];

    public override WiredValidation? Validate(FurniWired settings)
    {
        if (WiredLinkDataframe.Utf8Length(settings.StringParam) > MaxMessageBytes)
            return WiredValidation.Of("wiredfurni.params.link.message_too_large");
        return WiredLinks.RequireLinks(settings);
    }

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        var message = box.IntParam(Source) == CustomMessage ? run.Fill(box.Settings.StringParam) : run.TriggerText;
        if (message is null)
            return;

        foreach (var link in run.Furni(box).Where(WiredFurni.IsLink).Take(WiredLinks.MaxLinksPerRun).ToList())
            await run.Room.SendLinkAsync(link, dataframe: false, message, [], run);
    }
}
